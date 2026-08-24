namespace ZARI.Application.Features.Inventory.Items.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Items.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateItemCommandHandler(IAppDbContext dbContext) : ICommandHandler<CreateItemCommand, Result<ItemResponse>>
{
    public async Task<Result<ItemResponse>> HandleAsync(CreateItemCommand command, CancellationToken cancellationToken = default)
    {
        var codeExists = await dbContext.Items.AnyAsync(i => i.Code == command.Code, cancellationToken);
        if (codeExists)
            return Result.Failure<ItemResponse>(Error.Conflict("Item.DuplicateCode", $"An item with code '{command.Code}' already exists."));

        var uomExists = await dbContext.Uoms.AnyAsync(u => u.Id == command.BaseUomId, cancellationToken);
        if (!uomExists)
            return Result.Failure<ItemResponse>(Error.NotFound("Uom.NotFound", $"UOM with ID '{command.BaseUomId}' was not found."));

        if (command.CategoryId is not null)
        {
            var categoryExists = await dbContext.ItemCategories.AnyAsync(c => c.Id == command.CategoryId, cancellationToken);
            if (!categoryExists)
                return Result.Failure<ItemResponse>(Error.NotFound("ItemCategory.NotFound", $"Item category with ID '{command.CategoryId}' was not found."));
        }

        var item = new Item
        {
            Code = command.Code,
            Name = command.Name,
            Description = command.Description,
            CategoryId = command.CategoryId,
            BaseUomId = command.BaseUomId,
            ItemType = command.ItemType,
            CostingMethod = command.CostingMethod,
            IsSerialized = command.IsSerialized,
            IsBatchTracked = command.IsBatchTracked,
            IsSold = command.IsSold,
            IsPurchased = command.IsPurchased,
            IsStocked = command.IsStocked,
            SalesAccountId = command.SalesAccountId,
            PurchaseAccountId = command.PurchaseAccountId,
            InventoryAccountId = command.InventoryAccountId,
            CogsAccountId = command.CogsAccountId,
            Status = command.Status
        };

        dbContext.Items.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new ItemResponse(
            item.Id, item.Code, item.Name, item.Description, item.CategoryId, item.BaseUomId, item.ItemType, item.CostingMethod,
            item.IsSerialized, item.IsBatchTracked, item.IsSold, item.IsPurchased, item.IsStocked,
            item.SalesAccountId, item.PurchaseAccountId, item.InventoryAccountId, item.CogsAccountId,
            item.Status, item.CreatedAt);

        return Result.Success(response);
    }
}
