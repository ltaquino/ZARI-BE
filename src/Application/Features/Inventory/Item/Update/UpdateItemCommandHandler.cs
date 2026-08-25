namespace ZARI.Application.Features.Inventory.Items.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateItemCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<UpdateItemCommand>
{
    public async Task<Result> HandleAsync(UpdateItemCommand command, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.Items.FindAsync([command.Id], cancellationToken);
        if (item is null)
            return Result.Failure(Error.NotFound("Item.NotFound", $"Item with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("ITEMS", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("Item.Forbidden", "You do not have permission to update items."));

        var duplicateCode = await dbContext.Items.AnyAsync(i => i.Id != command.Id && i.Code == command.Code, cancellationToken);
        if (duplicateCode)
            return Result.Failure(Error.Conflict("Item.DuplicateCode", $"An item with code '{command.Code}' already exists."));

        var uomExists = await dbContext.Uoms.AnyAsync(u => u.Id == command.BaseUomId, cancellationToken);
        if (!uomExists)
            return Result.Failure(Error.NotFound("Uom.NotFound", $"UOM with ID '{command.BaseUomId}' was not found."));

        if (command.CategoryId is not null)
        {
            var categoryExists = await dbContext.ItemCategories.AnyAsync(c => c.Id == command.CategoryId, cancellationToken);
            if (!categoryExists)
                return Result.Failure(Error.NotFound("ItemCategory.NotFound", $"Item category with ID '{command.CategoryId}' was not found."));
        }

        item.Code = command.Code;
        item.Name = command.Name;
        item.Description = command.Description;
        item.CategoryId = command.CategoryId;
        item.BaseUomId = command.BaseUomId;
        item.ItemType = command.ItemType;
        item.CostingMethod = command.CostingMethod;
        item.IsSerialized = command.IsSerialized;
        item.IsBatchTracked = command.IsBatchTracked;
        item.IsSold = command.IsSold;
        item.IsPurchased = command.IsPurchased;
        item.IsStocked = command.IsStocked;
        item.SalesAccountId = command.SalesAccountId;
        item.PurchaseAccountId = command.PurchaseAccountId;
        item.InventoryAccountId = command.InventoryAccountId;
        item.CogsAccountId = command.CogsAccountId;
        item.Status = command.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
