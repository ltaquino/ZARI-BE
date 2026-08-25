namespace ZARI.Application.Features.Inventory.ItemBranchSettings.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateItemBranchSettingCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<UpdateItemBranchSettingCommand>
{
    public async Task<Result> HandleAsync(UpdateItemBranchSettingCommand command, CancellationToken cancellationToken = default)
    {
        var setting = await dbContext.ItemBranchSettings.FindAsync([command.Id], cancellationToken);
        if (setting is null)
            return Result.Failure(Error.NotFound("ItemBranchSetting.NotFound", $"Reorder setting with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("ITEM_BRANCH_SETTINGS", FormAction.Edit, setting.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("ItemBranchSetting.Forbidden", "You do not have permission to update item branch settings for this branch."));

        var itemExists = await dbContext.Items.AnyAsync(i => i.Id == command.ItemId, cancellationToken);
        if (!itemExists)
            return Result.Failure(Error.NotFound("Item.NotFound", $"Item with ID '{command.ItemId}' was not found."));

        var clashExists = await dbContext.ItemBranchSettings
            .AnyAsync(s => s.Id != command.Id && s.ItemId == command.ItemId && s.BranchId == command.BranchId, cancellationToken);

        if (clashExists)
            return Result.Failure(Error.Conflict("ItemBranchSetting.Duplicate", "A reorder setting for this item and branch already exists — edit it instead."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

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
