namespace ZARI.Application.Features.Purchasing.PurchaseOrders.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseOrders.GetAll;
using ZARI.Application.Features.Purchasing.PurchaseOrders.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreatePurchaseOrderCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CreatePurchaseOrderCommand, Result<PurchaseOrderResponse>>
{
    public async Task<Result<PurchaseOrderResponse>> HandleAsync(CreatePurchaseOrderCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("PURCHASE_ORDERS", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<PurchaseOrderResponse>(Error.Forbidden("PurchaseOrder.Forbidden", "You do not have permission to create purchase orders for this branch."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<PurchaseOrderResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var supplier = await dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == command.SupplierId, cancellationToken);
        if (supplier is null)
            return Result.Failure<PurchaseOrderResponse>(Error.NotFound("Supplier.NotFound", $"Supplier with ID '{command.SupplierId}' was not found."));

        PurchaseRequest? purchaseRequest = null;
        if (command.PurchaseRequestId is not null)
        {
            purchaseRequest = await dbContext.PurchaseRequests
                .Include(r => r.Lines).ThenInclude(l => l.Item)
                .FirstOrDefaultAsync(r => r.Id == command.PurchaseRequestId, cancellationToken);
            if (purchaseRequest is null)
                return Result.Failure<PurchaseOrderResponse>(Error.NotFound("PurchaseRequest.NotFound", $"Purchase request with ID '{command.PurchaseRequestId}' was not found."));

            if (purchaseRequest.Status != "APPROVED")
                return Result.Failure<PurchaseOrderResponse>(Error.Validation("PurchaseOrder.PurchaseRequestNotApproved", "The referenced purchase request must be approved before a purchase order can be created against it."));
        }
        else if (command.Lines.Any(l => l.PurchaseRequestLineId.HasValue))
        {
            return Result.Failure<PurchaseOrderResponse>(Error.Validation("PurchaseOrder.UnexpectedPurchaseRequestLine", "Lines cannot reference a purchase request line unless this order itself references a purchase request."));
        }

        if (purchaseRequest is not null)
        {
            var referencedLineIds = command.Lines.Where(l => l.PurchaseRequestLineId.HasValue).Select(l => l.PurchaseRequestLineId!.Value).Distinct().ToList();
            var alreadyOrdered = await dbContext.PurchaseOrderLines
                .Where(l => l.PurchaseRequestLineId.HasValue && referencedLineIds.Contains(l.PurchaseRequestLineId.Value) && l.PurchaseOrder.Status == "POSTED")
                .GroupBy(l => l.PurchaseRequestLineId!.Value)
                .Select(g => new { PurchaseRequestLineId = g.Key, Qty = g.Sum(l => l.Qty) })
                .ToDictionaryAsync(x => x.PurchaseRequestLineId, x => x.Qty, cancellationToken);

            var validationResult = ValidateAgainstPurchaseRequest(purchaseRequest, command.Lines, alreadyOrdered);
            if (!validationResult.IsSuccess)
                return Result.Failure<PurchaseOrderResponse>(validationResult.Error!);
        }

        var itemIds = command.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
        if (items.Count != itemIds.Count)
            return Result.Failure<PurchaseOrderResponse>(Error.NotFound("Item.NotFound", "One or more items on this purchase order were not found."));

        var uomIds = command.Lines.Select(l => l.UomId).Distinct().ToList();
        var uoms = await dbContext.Uoms.Where(u => uomIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);
        if (uoms.Count != uomIds.Count)
            return Result.Failure<PurchaseOrderResponse>(Error.NotFound("Uom.NotFound", "One or more units of measure on this purchase order were not found."));

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(command.BranchId, "PO"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<PurchaseOrderResponse>(numberResult.Error!);

        var order = new PurchaseOrder
        {
            PoNo = numberResult.Value!.DocumentNumber,
            BranchId = command.BranchId,
            SupplierId = command.SupplierId,
            OrderDate = command.OrderDate,
            ExpectedDate = command.ExpectedDate,
            Status = "DRAFT",
            Remarks = command.Remarks,
            PurchaseRequestId = command.PurchaseRequestId,
            CreatedBy = command.CreatedBy,
            Lines = command.Lines.Select(l => new PurchaseOrderLine
            {
                ItemId = l.ItemId,
                Qty = l.Qty,
                UomId = l.UomId,
                UnitCost = l.UnitCost,
                PurchaseRequestLineId = l.PurchaseRequestLineId
            }).ToList()
        };

        dbContext.PurchaseOrders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        order.Supplier = supplier;
        foreach (var line in order.Lines)
        {
            line.Item = items[line.ItemId];
            line.Uom = uoms[line.UomId];
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("PURCHASE_ORDER", order.Id.ToString(), order.BranchId, "CREATED", "ACTIVITY",
                "created this purchase order", command.CreatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<PurchaseOrderResponse>(notifyResult.Error!);

        return Result.Success(PurchaseOrderMapper.ToResponse(order));
    }

    /// <summary>
    /// Structural checks (every line references a real line on the given request, for the same item)
    /// plus the actual cap: a purchase request line can't be ordered past its own QtyRequested once
    /// <paramref name="alreadyOrderedByPurchaseRequestLine"/> (every OTHER posted purchase order's
    /// claim on that line) is added to what this command itself is requesting.
    /// </summary>
    internal static Result ValidateAgainstPurchaseRequest(
        PurchaseRequest purchaseRequest, List<PurchaseOrderLineInput> lines, Dictionary<Guid, decimal> alreadyOrderedByPurchaseRequestLine)
    {
        if (lines.Any(l => !l.PurchaseRequestLineId.HasValue))
            return Result.Failure(Error.Validation("PurchaseOrder.LineMissingPurchaseRequestLine", "Every line must reference a specific purchase request line when this order is created against a purchase request."));

        var prLinesById = purchaseRequest.Lines.ToDictionary(l => l.Id);
        foreach (var line in lines)
        {
            if (!prLinesById.TryGetValue(line.PurchaseRequestLineId!.Value, out var prLine))
                return Result.Failure(Error.Validation("PurchaseOrder.InvalidPurchaseRequestLine", "One or more lines reference a purchase request line that doesn't belong to the referenced purchase request."));
            if (prLine.ItemId != line.ItemId)
                return Result.Failure(Error.Validation("PurchaseOrder.ItemMismatch", $"A line's item must match the purchase request line it references ('{prLine.Item.Code}')."));
        }

        foreach (var group in lines.GroupBy(l => l.PurchaseRequestLineId!.Value))
        {
            var prLine = prLinesById[group.Key];
            var remaining = prLine.QtyRequested - alreadyOrderedByPurchaseRequestLine.GetValueOrDefault(group.Key);
            var requested = group.Sum(l => l.Qty);
            if (requested > remaining)
                return Result.Failure(Error.Validation("PurchaseOrder.ExceedsRequestedQty", $"This order requests {requested} of '{prLine.Item.Code}' but only {remaining} of purchase request line quantity remains unordered."));
        }

        return Result.Success();
    }
}
