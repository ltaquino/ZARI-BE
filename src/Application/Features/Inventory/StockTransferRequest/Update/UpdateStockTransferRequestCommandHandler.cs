namespace ZARI.Application.Features.Inventory.StockTransferRequests.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Inventory.StockTransferRequests.GetAll;
using ZARI.Application.Features.Inventory.StockTransferRequests.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdateStockTransferRequestCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<UpdateStockTransferRequestCommand, Result<StockTransferRequestResponse>>
{
    public async Task<Result<StockTransferRequestResponse>> HandleAsync(UpdateStockTransferRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.StockTransferRequests
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (request is null)
            return Result.Failure<StockTransferRequestResponse>(Error.NotFound("StockTransferRequest.NotFound", $"Stock transfer request with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("STOCK_TRANSFER_REQUESTS", FormAction.Edit, request.DestBranchId, cancellationToken))
            return Result.Failure<StockTransferRequestResponse>(Error.Forbidden("StockTransferRequest.Forbidden", "You do not have permission to edit this stock transfer request for the requesting branch."));

        if (request.Status != "DRAFT")
            return Result.Failure<StockTransferRequestResponse>(Error.Validation("StockTransferRequest.NotDraft", "Only draft stock transfer requests can be edited."));

        var sourceWarehouseExists = await dbContext.Warehouses.AnyAsync(w => w.Id == command.SourceWarehouseId, cancellationToken);
        if (!sourceWarehouseExists)
            return Result.Failure<StockTransferRequestResponse>(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{command.SourceWarehouseId}' was not found."));

        var destWarehouseExists = await dbContext.Warehouses.AnyAsync(w => w.Id == command.DestWarehouseId, cancellationToken);
        if (!destWarehouseExists)
            return Result.Failure<StockTransferRequestResponse>(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{command.DestWarehouseId}' was not found."));

        var sourceBranchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.SourceBranchId, cancellationToken);
        if (!sourceBranchExists)
            return Result.Failure<StockTransferRequestResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.SourceBranchId}' was not found."));

        var destBranchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.DestBranchId, cancellationToken);
        if (!destBranchExists)
            return Result.Failure<StockTransferRequestResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.DestBranchId}' was not found."));

        var itemIds = command.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
        if (items.Count != itemIds.Count)
            return Result.Failure<StockTransferRequestResponse>(Error.NotFound("Item.NotFound", "One or more items on this request were not found."));

        var uomIds = command.Lines.Select(l => l.UomId).Distinct().ToList();
        var uoms = await dbContext.Uoms.Where(u => uomIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);
        if (uoms.Count != uomIds.Count)
            return Result.Failure<StockTransferRequestResponse>(Error.NotFound("Uom.NotFound", "One or more units of measure on this request were not found."));

        request.SourceBranchId = command.SourceBranchId;
        request.SourceWarehouseId = command.SourceWarehouseId;
        request.DestBranchId = command.DestBranchId;
        request.DestWarehouseId = command.DestWarehouseId;
        request.RequestDate = command.RequestDate;
        request.Remarks = command.Remarks;

        request.Lines.Clear();
        foreach (var line in command.Lines)
        {
            request.Lines.Add(new StockTransferRequestLine
            {
                ItemId = line.ItemId,
                QtyRequested = line.QtyRequested,
                UomId = line.UomId
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var line in request.Lines)
        {
            line.Item = items[line.ItemId];
            line.Uom = uoms[line.UomId];
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("STOCK_TRANSFER_REQUEST", request.Id.ToString(), request.DestBranchId, "UPDATED", "ACTIVITY",
                "updated this stock transfer request", command.UpdatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<StockTransferRequestResponse>(notifyResult.Error!);

        return Result.Success(StockTransferRequestMapper.ToResponse(request));
    }
}
