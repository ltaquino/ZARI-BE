namespace ZARI.Application.Features.Inventory.StockTransferRequests.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Inventory.StockTransferRequests.GetAll;
using ZARI.Application.Features.Inventory.StockTransferRequests.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateStockTransferRequestCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CreateStockTransferRequestCommand, Result<StockTransferRequestResponse>>
{
    public async Task<Result<StockTransferRequestResponse>> HandleAsync(CreateStockTransferRequestCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("STOCK_TRANSFER_REQUESTS", FormAction.Create, command.DestBranchId, cancellationToken))
            return Result.Failure<StockTransferRequestResponse>(Error.Forbidden("StockTransferRequest.Forbidden", "You do not have permission to create stock transfer requests for this branch."));

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

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(command.DestBranchId, "STR"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<StockTransferRequestResponse>(numberResult.Error!);

        var request = new StockTransferRequest
        {
            RequestNo = numberResult.Value!.DocumentNumber,
            SourceBranchId = command.SourceBranchId,
            SourceWarehouseId = command.SourceWarehouseId,
            DestBranchId = command.DestBranchId,
            DestWarehouseId = command.DestWarehouseId,
            RequestDate = command.RequestDate,
            Status = "DRAFT",
            Remarks = command.Remarks,
            CreatedBy = command.CreatedBy,
            Lines = command.Lines.Select(l => new StockTransferRequestLine
            {
                ItemId = l.ItemId,
                QtyRequested = l.QtyRequested,
                UomId = l.UomId
            }).ToList()
        };

        dbContext.StockTransferRequests.Add(request);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var line in request.Lines)
        {
            line.Item = items[line.ItemId];
            line.Uom = uoms[line.UomId];
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("STOCK_TRANSFER_REQUEST", request.Id.ToString(), request.DestBranchId, "CREATED", "ACTIVITY",
                "created this stock transfer request", command.CreatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<StockTransferRequestResponse>(notifyResult.Error!);

        return Result.Success(StockTransferRequestMapper.ToResponse(request));
    }
}
