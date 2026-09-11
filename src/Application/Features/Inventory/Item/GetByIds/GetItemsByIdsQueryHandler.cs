namespace ZARI.Application.Features.Inventory.Items.GetByIds;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Items.Get;
using ZARI.Domain.Common;

/// <summary>
/// Resolves a small, caller-specified set of item ids to full rows — what every "show this
/// document's own lines" screen uses instead of loading the whole item catalog just to find a
/// handful of names/codes/flags for items it already knows the ids of.
/// </summary>
public sealed class GetItemsByIdsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetItemsByIdsQuery, Result<List<ItemResponse>>>
{
    public async Task<Result<List<ItemResponse>>> HandleAsync(GetItemsByIdsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("ITEMS", FormAction.View, cancellationToken))
            return Result.Failure<List<ItemResponse>>(Error.Forbidden("Item.Forbidden", "You do not have permission to view items."));

        var ids = (query.Ids ?? []).Distinct().ToList();
        if (ids.Count == 0)
            return Result.Success(new List<ItemResponse>());

        var items = await dbContext.Items.AsNoTracking()
            .Where(i => ids.Contains(i.Id))
            .Select(i => new ItemResponse(
                i.Id, i.Code, i.Name, i.Description, i.CategoryId, i.BaseUomId, i.ItemType, i.CostingMethod,
                i.IsSerialized, i.IsBatchTracked, i.IsSold, i.IsPurchased, i.IsStocked, i.IsTileDisplay, i.AllowNegativeStock,
                i.SalesAccountId, i.PurchaseAccountId, i.InventoryAccountId, i.CogsAccountId,
                i.VatType, i.Status, i.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
