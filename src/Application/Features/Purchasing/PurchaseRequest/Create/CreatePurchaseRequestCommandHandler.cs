namespace ZARI.Application.Features.Purchasing.PurchaseRequests.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Purchasing.PurchaseRequests.GetAll;
using ZARI.Application.Features.Purchasing.PurchaseRequests.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreatePurchaseRequestCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CreatePurchaseRequestCommand, Result<PurchaseRequestResponse>>
{
    public async Task<Result<PurchaseRequestResponse>> HandleAsync(CreatePurchaseRequestCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("PURCHASE_REQUESTS", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<PurchaseRequestResponse>(Error.Forbidden("PurchaseRequest.Forbidden", "You do not have permission to create purchase requests for this branch."));

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

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(command.BranchId, "PR"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<PurchaseRequestResponse>(numberResult.Error!);

        var request = new PurchaseRequest
        {
            RequestNo = numberResult.Value!.DocumentNumber,
            BranchId = command.BranchId,
            RequestDate = command.RequestDate,
            Status = "DRAFT",
            Remarks = command.Remarks,
            CreatedBy = command.CreatedBy,
            Lines = command.Lines.Select(l => new PurchaseRequestLine
            {
                ItemId = l.ItemId,
                QtyRequested = l.QtyRequested,
                UomId = l.UomId,
                NeededByDate = l.NeededByDate
            }).ToList()
        };

        dbContext.PurchaseRequests.Add(request);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var line in request.Lines)
        {
            line.Item = items[line.ItemId];
            line.Uom = uoms[line.UomId];
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("PURCHASE_REQUEST", request.Id.ToString(), request.BranchId, "CREATED", "ACTIVITY",
                "created this purchase request", command.CreatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<PurchaseRequestResponse>(notifyResult.Error!);

        return Result.Success(PurchaseRequestMapper.ToResponse(request));
    }
}
