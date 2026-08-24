namespace ZARI.Application.Features.Inventory.ItemBranchSettings.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateItemBranchSettingCommandHandler(IAppDbContext dbContext) : ICommandHandler<UpdateItemBranchSettingCommand>
{
    public async Task<Result> HandleAsync(UpdateItemBranchSettingCommand command, CancellationToken cancellationToken = default)
    {
        var setting = await dbContext.ItemBranchSettings.FindAsync([command.Id], cancellationToken);
        if (setting is null)
            return Result.Failure(Error.NotFound("ItemBranchSetting.NotFound", $"Reorder setting with ID '{command.Id}' was not found."));

        var itemExists = await dbContext.Items.AnyAsync(i => i.Id == command.ItemId, cancellationToken);
        if (!itemExists)
            return Result.Failure(Error.NotFound("Item.NotFound", $"Item with ID '{command.ItemId}' was not found."));

        var clashExists = await dbContext.ItemBranchSettings
            .AnyAsync(s => s.Id != command.Id && s.ItemId == command.ItemId && s.BranchId == command.BranchId, cancellationToken);

        if (clashExists)
            return Result.Failure(Error.Conflict("ItemBranchSetting.Duplicate", "A reorder setting for this item and branch already exists — edit it instead."));

        if (command.DefaultWarehouseId is not null)
        {
            var warehouseExists = await dbContext.Warehouses.AnyAsync(w => w.Id == command.DefaultWarehouseId, cancellationToken);
            if (!warehouseExists)
                return Result.Failure(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{command.DefaultWarehouseId}' was not found."));
        }

        setting.ItemId = command.ItemId;
        setting.BranchId = command.BranchId;
        setting.DefaultWarehouseId = command.DefaultWarehouseId;
        setting.ReorderPoint = command.ReorderPoint;
        setting.MinStock = command.MinStock;
        setting.MaxStock = command.MaxStock;
        setting.Status = command.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
