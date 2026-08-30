namespace ZARI.Application.Features.Inventory.Items.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Items.Get;
using ZARI.Domain.Common;

public sealed class GetAllItemsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllItemsQuery, Result<List<ItemResponse>>>
{
    public async Task<Result<List<ItemResponse>>> HandleAsync(GetAllItemsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("ITEMS", FormAction.View, cancellationToken))
            return Result.Failure<List<ItemResponse>>(Error.Forbidden("Item.Forbidden", "You do not have permission to view items."));

        var items = await dbContext.Items
            .OrderBy(i => i.Code)
            .Select(i => new ItemResponse(
                i.Id, i.Code, i.Name, i.Description, i.CategoryId, i.BaseUomId, i.ItemType, i.CostingMethod,
                i.IsSerialized, i.IsBatchTracked, i.IsSold, i.IsPurchased, i.IsStocked,
                i.SalesAccountId, i.PurchaseAccountId, i.InventoryAccountId, i.CogsAccountId,
                i.VatType, i.Status, i.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
