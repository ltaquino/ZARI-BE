namespace ZARI.Application.Features.Inventory.ItemBranchSettings.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.ItemBranchSettings.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateItemBranchSettingCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<CreateItemBranchSettingCommand, Result<ItemBranchSettingResponse>>
{
    public async Task<Result<ItemBranchSettingResponse>> HandleAsync(CreateItemBranchSettingCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("ITEM_BRANCH_SETTINGS", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<ItemBranchSettingResponse>(Error.Forbidden("ItemBranchSetting.Forbidden", "You do not have permission to create item branch settings for this branch."));

        var itemExists = await dbContext.Items.AnyAsync(i => i.Id == command.ItemId, cancellationToken);
        if (!itemExists)
            return Result.Failure<ItemBranchSettingResponse>(Error.NotFound("Item.NotFound", $"Item with ID '{command.ItemId}' was not found."));

        var clashExists = await dbContext.ItemBranchSettings
            .AnyAsync(s => s.ItemId == command.ItemId && s.BranchId == command.BranchId, cancellationToken);

        if (clashExists)
            return Result.Failure<ItemBranchSettingResponse>(Error.Conflict("ItemBranchSetting.Duplicate", "A reorder setting for this item and branch already exists — edit it instead."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<ItemBranchSettingResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        if (command.DefaultWarehouseId is not null)
        {
            var warehouseExists = await dbContext.Warehouses.AnyAsync(w => w.Id == command.DefaultWarehouseId, cancellationToken);
            if (!warehouseExists)
                return Result.Failure<ItemBranchSettingResponse>(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{command.DefaultWarehouseId}' was not found."));
        }

        var setting = new ItemBranchSetting
        {
            ItemId = command.ItemId,
            BranchId = command.BranchId,
            DefaultWarehouseId = command.DefaultWarehouseId,
            ReorderPoint = command.ReorderPoint,
            MinStock = command.MinStock,
            MaxStock = command.MaxStock,
            Status = command.Status
        };

        dbContext.ItemBranchSettings.Add(setting);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new ItemBranchSettingResponse(setting.Id, setting.ItemId, setting.BranchId, setting.DefaultWarehouseId, setting.ReorderPoint, setting.MinStock, setting.MaxStock, setting.Status, setting.CreatedAt);
        return Result.Success(response);
    }
}
