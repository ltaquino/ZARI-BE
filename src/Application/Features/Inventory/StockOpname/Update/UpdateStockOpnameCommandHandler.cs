namespace ZARI.Application.Features.Inventory.StockOpnames.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockOpnames.GetAll;
using ZARI.Application.Features.Inventory.StockOpnames.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdateStockOpnameCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler)
    : ICommandHandler<UpdateStockOpnameCommand, Result<StockOpnameResponse>>
{
    public async Task<Result<StockOpnameResponse>> HandleAsync(UpdateStockOpnameCommand command, CancellationToken cancellationToken = default)
    {
        var opname = await dbContext.StockOpnames
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == command.Id, cancellationToken);

        if (opname is null)
            return Result.Failure<StockOpnameResponse>(Error.NotFound("StockOpname.NotFound", $"Stock opname with ID '{command.Id}' was not found."));

        if (opname.Status != "DRAFT")
            return Result.Failure<StockOpnameResponse>(Error.Validation("StockOpname.NotDraft", "Only a draft stock count can be edited."));

        var warehouseExists = await dbContext.Warehouses.AnyAsync(w => w.Id == command.WarehouseId, cancellationToken);
        if (!warehouseExists)
            return Result.Failure<StockOpnameResponse>(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{command.WarehouseId}' was not found."));

        var itemIds = command.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Include(i => i.BaseUom).Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
        if (items.Count != itemIds.Count)
            return Result.Failure<StockOpnameResponse>(Error.NotFound("Item.NotFound", "One or more items on this stock count were not found."));

        opname.BranchId = command.BranchId;
        opname.WarehouseId = command.WarehouseId;
        opname.CountDate = command.CountDate;
        opname.Remarks = command.Remarks;

        opname.Lines.Clear();
        foreach (var line in command.Lines)
        {
            opname.Lines.Add(new StockOpnameLine
            {
                ItemId = line.ItemId,
                BatchNo = line.BatchNo,
                SerialNo = line.SerialNo,
                SystemQty = line.SystemQty,
                CountedQty = line.CountedQty,
                VarianceQty = line.CountedQty - line.SystemQty,
                UnitCost = line.UnitCost
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var line in opname.Lines)
        {
            line.Item = items[line.ItemId];
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("STOCK_OPNAME", opname.Id.ToString(), opname.BranchId, "UPDATED", "ACTIVITY",
                "updated this stock count", command.UpdatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<StockOpnameResponse>(notifyResult.Error!);

        return Result.Success(StockOpnameMapper.ToResponse(opname));
    }
}
