namespace ZARI.Application.Features.Inventory.Items.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetItemQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetItemQuery, Result<ItemResponse>>
{
    public async Task<Result<ItemResponse>> HandleAsync(GetItemQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("ITEMS", FormAction.View, cancellationToken))
            return Result.Failure<ItemResponse>(Error.Forbidden("Item.Forbidden", "You do not have permission to view items."));

        var item = await dbContext.Items
            .Where(i => i.Id == query.Id)
            .Select(i => new ItemResponse(
                i.Id, i.Code, i.Name, i.Description, i.CategoryId, i.BaseUomId, i.ItemType, i.CostingMethod,
                i.IsSerialized, i.IsBatchTracked, i.IsSold, i.IsPurchased, i.IsStocked,
                i.SalesAccountId, i.PurchaseAccountId, i.InventoryAccountId, i.CogsAccountId,
                i.VatType, i.Status, i.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
            return Result.Failure<ItemResponse>(Error.NotFound("Item.NotFound", $"Item with ID '{query.Id}' was not found."));

        return Result.Success(item);
    }
}
