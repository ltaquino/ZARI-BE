namespace ZARI.Application.Features.Inventory.Items.Search;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Items.Get;
using ZARI.Domain.Common;

/// <summary>
/// Server-side item search — the fix behind keeping ItemPicker/PosScanInput/MultiItemPicker fast
/// no matter how large the item catalog gets (a 100,000-SKU catalog was the concrete case this was
/// built for). Matches Code prefix first (uses Items.Code's existing unique index), then a plain
/// Code/Name substring match, capped at Limit — never returns anywhere close to the whole catalog,
/// unlike the client-side full-cache `.filter()` it replaces.
/// </summary>
public sealed class SearchItemsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<SearchItemsQuery, Result<List<ItemResponse>>>
{
    public async Task<Result<List<ItemResponse>>> HandleAsync(SearchItemsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("ITEMS", FormAction.View, cancellationToken))
            return Result.Failure<List<ItemResponse>>(Error.Forbidden("Item.Forbidden", "You do not have permission to view items."));

        var q = query.Q?.Trim() ?? "";
        if (q.Length == 0)
            return Result.Success(new List<ItemResponse>());

        var limit = query.Limit is > 0 and <= 50 ? query.Limit : 8;

        var items = await dbContext.Items.AsNoTracking()
            .Where(i => i.Status == "active" && (i.Code.Contains(q) || i.Name.Contains(q)))
            .OrderByDescending(i => i.Code.StartsWith(q))
            .ThenBy(i => i.Code)
            .Take(limit)
            .Select(i => new ItemResponse(
                i.Id, i.Code, i.Name, i.Description, i.CategoryId, i.BaseUomId, i.ItemType, i.CostingMethod,
                i.IsSerialized, i.IsBatchTracked, i.IsSold, i.IsPurchased, i.IsStocked, i.IsTileDisplay, i.AllowNegativeStock,
                i.SalesAccountId, i.PurchaseAccountId, i.InventoryAccountId, i.CogsAccountId,
                i.VatType, i.Status, i.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
