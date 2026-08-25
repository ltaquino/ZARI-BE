namespace ZARI.Application.Features.Purchasing.PurchaseRequests.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Purchasing.PurchaseRequests.GetAll;
using ZARI.Application.Features.Purchasing.PurchaseRequests.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdatePurchaseRequestCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<UpdatePurchaseRequestCommand, Result<PurchaseRequestResponse>>
{
    public async Task<Result<PurchaseRequestResponse>> HandleAsync(UpdatePurchaseRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.PurchaseRequests
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (request is null)
            return Result.Failure<PurchaseRequestResponse>(Error.NotFound("PurchaseRequest.NotFound", $"Purchase request with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("PURCHASE_REQUESTS", FormAction.Edit, request.BranchId, cancellationToken))
            return Result.Failure<PurchaseRequestResponse>(Error.Forbidden("PurchaseRequest.Forbidden", "You do not have permission to edit this purchase request for this branch."));

        if (request.Status != "DRAFT")
            return Result.Failure<PurchaseRequestResponse>(Error.Validation("PurchaseRequest.NotDraft", "Only draft purchase requests can be edited."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<PurchaseRequestResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var itemIds = command.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
        if (items.Count != itemIds.Count)
            return Result.Failure<PurchaseRequestResponse>(Error.NotFound("Item.NotFound", "One or more items on this request were not found."));

        var uomIds = command.Lines.Select(l => l.UomId).Distinct().ToList();
        var uoms = await dbContext.Uoms.Where(u => uomIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);
        if (uoms.Count != uomIds.Count)
            return Result.Failure<PurchaseRequestResponse>(Error.NotFound("Uom.NotFound", "One or more units of measure on this request were not found."));

        request.BranchId = command.BranchId;
        request.RequestDate = command.RequestDate;
        request.Remarks = command.Remarks;

        request.Lines.Clear();
        foreach (var line in command.Lines)
        {
            request.Lines.Add(new PurchaseRequestLine
            {
                ItemId = line.ItemId,
                QtyRequested = line.QtyRequested,
                UomId = line.UomId,
                NeededByDate = line.NeededByDate
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var line in request.Lines)
        {
            line.Item = items[line.ItemId];
            line.Uom = uoms[line.UomId];
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("PURCHASE_REQUEST", request.Id.ToString(), request.BranchId, "UPDATED", "ACTIVITY",
                "updated this purchase request", command.UpdatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<PurchaseRequestResponse>(notifyResult.Error!);

        return Result.Success(PurchaseRequestMapper.ToResponse(request));
    }
}
