namespace ZARI.Application.Features.Inventory.GoodsReceipts.Approve;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Inventory.GoodsReceipts.GetAll;
using ZARI.Application.Features.Inventory.GoodsReceipts.Shared;
using ZARI.Application.Features.Inventory.SerialNumbers.GetAll;
using ZARI.Application.Features.Inventory.SerialNumbers.Receive;
using ZARI.Application.Features.Inventory.StockLedgers.Receive;
using ZARI.Application.Features.Inventory.StockLocationBalances.Receive;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// PENDING_APPROVAL -> POSTED. Approving IS the "checked by" record (see ApprovalAction).
/// Mirrors the FE prototype's approveGoodsReceipt orchestration (data/inventory/goodsReceipts.ts):
/// decide the approval request, post every line to the stock ledger, receive serials/location
/// balances, then post the balancing GL journal. Reuses every already-migrated engine via injected
/// handlers rather than duplicating their logic — same composition pattern as
/// PostGlJournalCommandHandler reusing GetNextDocumentNumberCommandHandler.
/// </summary>
public sealed class ApproveGoodsReceiptCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<ReceiveStockCommand, Result<ReceiveStockResponse>> receiveStockHandler,
    ICommandHandler<ReceiveSerialCommand, Result<SerialNumberResponse>> receiveSerialHandler,
    ICommandHandler<ReceiveIntoLocationCommand, Result> receiveIntoLocationHandler,
    ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler)
    : ICommandHandler<ApproveGoodsReceiptCommand, Result<GoodsReceiptResponse>>
{
    public async Task<Result<GoodsReceiptResponse>> HandleAsync(ApproveGoodsReceiptCommand command, CancellationToken cancellationToken = default)
    {
        var receipt = await dbContext.GoodsReceipts
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (receipt is null)
            return Result.Failure<GoodsReceiptResponse>(Error.NotFound("GoodsReceipt.NotFound", $"Goods receipt with ID '{command.Id}' was not found."));

        if (receipt.Status != "PENDING_APPROVAL")
            return Result.Failure<GoodsReceiptResponse>(Error.Validation("GoodsReceipt.NotPendingApproval", "Only goods receipts pending approval can be approved."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "GOODS_RECEIPT" && r.EntityId == receipt.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<GoodsReceiptResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this goods receipt."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<GoodsReceiptResponse>(decideResult.Error!);

        foreach (var line in receipt.Lines)
        {
            var receiveResult = await receiveStockHandler.HandleAsync(
                new ReceiveStockCommand(line.ItemId, receipt.BranchId, receipt.WarehouseId, line.BatchNo, line.QtyReceived, line.UnitCost,
                    "GoodsReceiptLine", line.Id.ToString(), receipt.GrDate, null),
                cancellationToken);
            if (!receiveResult.IsSuccess)
                return Result.Failure<GoodsReceiptResponse>(receiveResult.Error!);

            if (line.Item.IsSerialized && !string.IsNullOrWhiteSpace(line.SerialNo))
            {
                var serialResult = await receiveSerialHandler.HandleAsync(new ReceiveSerialCommand(line.ItemId, line.SerialNo, receipt.WarehouseId), cancellationToken);
                if (!serialResult.IsSuccess)
                    return Result.Failure<GoodsReceiptResponse>(serialResult.Error!);
            }

            if (line.LocationId.HasValue)
            {
                var locationResult = await receiveIntoLocationHandler.HandleAsync(
                    new ReceiveIntoLocationCommand(line.ItemId, receipt.WarehouseId, line.LocationId.Value, line.BatchNo, line.QtyReceived), cancellationToken);
                if (!locationResult.IsSuccess)
                    return Result.Failure<GoodsReceiptResponse>(locationResult.Error!);
            }
        }

        var journalResult = await PostInventoryJournalAsync(receipt, cancellationToken);
        if (!journalResult.IsSuccess)
            return Result.Failure<GoodsReceiptResponse>(journalResult.Error!);

        // ReceiveStockCommand/ReceiveIntoLocationCommand each run their own retryable transaction
        // and call ChangeTracker.Clear() at the start of every attempt (see StockBalanceLocker
        // usage) — that detaches the `receipt` this handler loaded earlier, so mutating it and
        // calling SaveChangesAsync would silently persist nothing. ExecuteUpdateAsync writes
        // directly, independent of whatever the tracker currently holds.
        receipt.Status = "POSTED";
        await dbContext.GoodsReceipts.Where(r => r.Id == receipt.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.Status, "POSTED"), cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("GOODS_RECEIPT", receipt.Id.ToString(), receipt.BranchId, "APPROVED", "ACTIVITY",
                "approved this goods receipt", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<GoodsReceiptResponse>(notifyResult.Error!);

        return Result.Success(GoodsReceiptMapper.ToResponse(receipt));
    }

    private async Task<Result> PostInventoryJournalAsync(GoodsReceipt receipt, CancellationToken cancellationToken)
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

        var isTransfer = receipt.ReceiptType == "TRANSFER_IN";
        var creditAccountResult = isTransfer
            ? await GetDefaultAccountIdAsync("1450", "Inventory In-Transit", cancellationToken)
            : await ResolveVarianceAccountIdAsync(receipt.ReasonCode, cancellationToken);
        if (!creditAccountResult.IsSuccess)
            return Result.Failure(creditAccountResult.Error!);

        var lines = debitsByAccount
            .Select(kv => new PostGlJournalLineInput(kv.Key, null, kv.Value, 0, null))
            .Append(new PostGlJournalLineInput(creditAccountResult.Value, null, 0, totalValue, null))
            .ToList();

        var description = $"Goods Receipt {receipt.GrNo} — {(isTransfer ? "transfer in" : "manual receipt")}";
        var postResult = await postGlJournalHandler.HandleAsync(
            new PostGlJournalCommand(receipt.BranchId, receipt.GrDate, "GoodsReceipt", receipt.Id.ToString(), description, lines), cancellationToken);
        return postResult.IsSuccess ? Result.Success() : Result.Failure(postResult.Error!);
    }

    private async Task<Result<Guid>> GetDefaultAccountIdAsync(string code, string label, CancellationToken cancellationToken)
    {
        var accountId = await dbContext.GlAccounts.Where(a => a.Code == code).Select(a => (Guid?)a.Id).FirstOrDefaultAsync(cancellationToken);
        return accountId is null
            ? Result.Failure<Guid>(Error.NotFound("GlAccount.NotFound", $"Default GL account '{label}' ({code}) is not configured — check the seeded chart of accounts."))
            : Result.Success(accountId.Value);
    }

    /// The GL account a reason's variance posts to, falling back to the default Inventory Variance account.
    private async Task<Result<Guid>> ResolveVarianceAccountIdAsync(string? reasonCode, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(reasonCode))
        {
            var glAccountId = await dbContext.AdjustmentReasons.Where(r => r.Code == reasonCode).Select(r => r.GlAccountId).FirstOrDefaultAsync(cancellationToken);
            if (Guid.TryParse(glAccountId, out var explicitAccountId))
                return Result.Success(explicitAccountId);
        }

        return await GetDefaultAccountIdAsync("5100", "Inventory Variance / Shrinkage", cancellationToken);
    }
}
