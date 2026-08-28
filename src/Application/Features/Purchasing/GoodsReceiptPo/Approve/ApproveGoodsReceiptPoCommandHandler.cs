namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.Approve;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.Create;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.GetAll;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.Shared;
using ZARI.Application.Features.Inventory.SerialNumbers.GetAll;
using ZARI.Application.Features.Inventory.SerialNumbers.Receive;
using ZARI.Application.Features.Inventory.StockLedgers.Receive;
using ZARI.Application.Features.Inventory.StockLocationBalances.Receive;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// PENDING_APPROVAL -> POSTED. Mirrors GoodsReceipt's approve orchestration (receive stock,
/// serials, location balances, then post a GL journal) via the same shared engines — but the
/// credit side is always the "2100" Goods Received Not Invoiced holding account, since a GRPO is
/// always sourced externally from a vendor (no in-transit/variance branching like GoodsReceipt).
/// </summary>
public sealed class ApproveGoodsReceiptPoCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<ReceiveStockCommand, Result<ReceiveStockResponse>> receiveStockHandler,
    ICommandHandler<ReceiveSerialCommand, Result<SerialNumberResponse>> receiveSerialHandler,
    ICommandHandler<ReceiveIntoLocationCommand, Result> receiveIntoLocationHandler,
    ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveGoodsReceiptPoCommand, Result<GoodsReceiptPoResponse>>
{
    public async Task<Result<GoodsReceiptPoResponse>> HandleAsync(ApproveGoodsReceiptPoCommand command, CancellationToken cancellationToken = default)
    {
        var receipt = await dbContext.GoodsReceiptPos
            .Include(r => r.Supplier)
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (receipt is null)
            return Result.Failure<GoodsReceiptPoResponse>(Error.NotFound("GoodsReceiptPo.NotFound", $"Goods receipt (PO) with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("GOODS_RECEIPT_PO", FormAction.Approve, receipt.BranchId, cancellationToken))
            return Result.Failure<GoodsReceiptPoResponse>(Error.Forbidden("GoodsReceiptPo.Forbidden", "You do not have permission to approve goods receipts (PO) for this branch."));

        if (receipt.Status != "PENDING_APPROVAL")
            return Result.Failure<GoodsReceiptPoResponse>(Error.Validation("GoodsReceiptPo.NotPendingApproval", "Only goods receipts pending approval can be approved."));

        // Authoritative re-check, closing the race a friendly Create/Update-time check can't: another
        // goods receipt against the same purchase order line may have been approved in between. This
        // receipt is still PENDING_APPROVAL (not POSTED) right now, so it's naturally excluded from
        // its own "already received" tally — same pattern as PurchaseOrder's Approve-time re-check.
        if (receipt.PurchaseOrderId is not null)
        {
            var purchaseOrder = await dbContext.PurchaseOrders
                .Include(p => p.Lines).ThenInclude(l => l.Item)
                .FirstOrDefaultAsync(p => p.Id == receipt.PurchaseOrderId, cancellationToken);
            if (purchaseOrder is not null)
            {
                var referencedLineIds = receipt.Lines.Where(l => l.PurchaseOrderLineId.HasValue).Select(l => l.PurchaseOrderLineId!.Value).Distinct().ToList();
                var alreadyReceived = await dbContext.GoodsReceiptPoLines
                    .Where(l => l.PurchaseOrderLineId.HasValue && referencedLineIds.Contains(l.PurchaseOrderLineId.Value) && l.GoodsReceiptPo.Status == "POSTED")
                    .GroupBy(l => l.PurchaseOrderLineId!.Value)
                    .Select(g => new { PurchaseOrderLineId = g.Key, QtyReceived = g.Sum(l => l.QtyReceived) })
                    .ToDictionaryAsync(x => x.PurchaseOrderLineId, x => x.QtyReceived, cancellationToken);

                var lineInputs = receipt.Lines.Select(l => new GoodsReceiptPoLineInput(l.ItemId, l.BatchNo, l.SerialNo, l.QtyReceived, l.UomId, l.UnitCost, l.LocationId, l.PurchaseOrderLineId)).ToList();
                var validationResult = CreateGoodsReceiptPoCommandHandler.ValidateAgainstPurchaseOrder(purchaseOrder, lineInputs, alreadyReceived);
                if (!validationResult.IsSuccess)
                    return Result.Failure<GoodsReceiptPoResponse>(validationResult.Error!);
            }
        }

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "GOODS_RECEIPT_PO" && r.EntityId == receipt.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<GoodsReceiptPoResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this goods receipt."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<GoodsReceiptPoResponse>(decideResult.Error!);

        foreach (var line in receipt.Lines)
        {
            var receiveResult = await receiveStockHandler.HandleAsync(
                new ReceiveStockCommand(line.ItemId, receipt.BranchId, receipt.WarehouseId, line.BatchNo, line.QtyReceived, line.UnitCost,
                    "GoodsReceiptPoLine", line.Id.ToString(), receipt.ReceiptDate, null),
                cancellationToken);
            if (!receiveResult.IsSuccess)
                return Result.Failure<GoodsReceiptPoResponse>(receiveResult.Error!);

            if (line.Item.IsSerialized && !string.IsNullOrWhiteSpace(line.SerialNo))
            {
                var serialResult = await receiveSerialHandler.HandleAsync(new ReceiveSerialCommand(line.ItemId, line.SerialNo, receipt.WarehouseId), cancellationToken);
                if (!serialResult.IsSuccess)
                    return Result.Failure<GoodsReceiptPoResponse>(serialResult.Error!);
            }

            if (line.LocationId.HasValue)
            {
                var locationResult = await receiveIntoLocationHandler.HandleAsync(
                    new ReceiveIntoLocationCommand(line.ItemId, receipt.WarehouseId, line.LocationId.Value, line.BatchNo, line.QtyReceived), cancellationToken);
                if (!locationResult.IsSuccess)
                    return Result.Failure<GoodsReceiptPoResponse>(locationResult.Error!);
            }
        }

        var journalResult = await PostGrniJournalAsync(receipt, cancellationToken);
        if (!journalResult.IsSuccess)
            return Result.Failure<GoodsReceiptPoResponse>(journalResult.Error!);

        // ReceiveStockCommand/ReceiveIntoLocationCommand each run their own retryable transaction
        // and call ChangeTracker.Clear() at the start of every attempt — that detaches the
        // `receipt` this handler loaded earlier, so mutating it and calling SaveChangesAsync would
        // silently persist nothing. ExecuteUpdateAsync writes directly, independent of the tracker.
        receipt.Status = "POSTED";
        await dbContext.GoodsReceiptPos.Where(r => r.Id == receipt.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.Status, "POSTED"), cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("GOODS_RECEIPT_PO", receipt.Id.ToString(), receipt.BranchId, "APPROVED", "ACTIVITY",
                "approved this goods receipt", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<GoodsReceiptPoResponse>(notifyResult.Error!);

        return Result.Success(GoodsReceiptPoMapper.ToResponse(receipt));
    }

    private async Task<Result> PostGrniJournalAsync(GoodsReceiptPo receipt, CancellationToken cancellationToken)
    {
        var debitsByAccount = new Dictionary<Guid, decimal>();
        foreach (var line in receipt.Lines)
        {
            var accountResult = Guid.TryParse(line.Item.InventoryAccountId, out var explicitAccountId)
                ? Result.Success(explicitAccountId)
                : await GetDefaultAccountIdAsync("1400", "Inventory Asset", cancellationToken);
            if (!accountResult.IsSuccess)
                return Result.Failure(accountResult.Error!);

            var amount = Math.Round(line.QtyReceived * line.UnitCost, 4);
            debitsByAccount[accountResult.Value] = debitsByAccount.GetValueOrDefault(accountResult.Value) + amount;
        }

        var totalValue = debitsByAccount.Values.Sum();
        if (totalValue <= 0)
            return Result.Success();

        var grniAccountResult = await GetDefaultAccountIdAsync("2100", "Goods Received Not Invoiced", cancellationToken);
        if (!grniAccountResult.IsSuccess)
            return Result.Failure(grniAccountResult.Error!);

        var lines = debitsByAccount
            .Select(kv => new PostGlJournalLineInput(kv.Key, receipt.CostCenterId, kv.Value, 0, null))
            .Append(new PostGlJournalLineInput(grniAccountResult.Value, receipt.CostCenterId, 0, totalValue, null))
            .ToList();

        var description = $"Goods Receipt (PO) {receipt.GrpoNo} — {receipt.Supplier.Name}";
        var postResult = await postGlJournalHandler.HandleAsync(
            new PostGlJournalCommand(receipt.BranchId, receipt.ReceiptDate, "PURCHASING", "GoodsReceiptPo", receipt.Id.ToString(), description, lines), cancellationToken);
        return postResult.IsSuccess ? Result.Success() : Result.Failure(postResult.Error!);
    }

    private async Task<Result<Guid>> GetDefaultAccountIdAsync(string code, string label, CancellationToken cancellationToken)
    {
        var accountId = await dbContext.GlAccounts.Where(a => a.Code == code).Select(a => (Guid?)a.Id).FirstOrDefaultAsync(cancellationToken);
        return accountId is null
            ? Result.Failure<Guid>(Error.NotFound("GlAccount.NotFound", $"Default GL account '{label}' ({code}) is not configured — check the seeded chart of accounts."))
            : Result.Success(accountId.Value);
    }
}
