namespace ZARI.Application.Features.Inventory.Items.TileMenu;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Items.Get;
using ZARI.Domain.Common;

/// <summary>
/// Server-side filtered list backing PosTileMenuModal's grid — IsSold + IsTileDisplay + active,
/// with an active ItemBranchSetting for this specific branch (the exact rule PosScanInput's
/// search already enforces). Replaces a client-side `getItemsSync().filter(...)` over the whole
/// catalog — a real cafe/restaurant tile menu is a curated handful of items, so this is always a
/// small, bounded result regardless of how large the overall item catalog is.
/// </summary>
public sealed class GetTileMenuItemsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetTileMenuItemsQuery, Result<List<TileMenuItemResponse>>>
{
    public async Task<Result<List<TileMenuItemResponse>>> HandleAsync(GetTileMenuItemsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("POS_MODE", FormAction.View, query.BranchId, cancellationToken)
            && !await permissionService.HasPermissionOnBranchAsync("POS_MODE", FormAction.Create, query.BranchId, cancellationToken))
            return Result.Failure<List<TileMenuItemResponse>>(Error.Forbidden("Item.Forbidden", "You do not have permission to use POS Mode for this branch."));

        // Ordered BEFORE projecting into the response shape — EF can't translate an OrderBy key
        // selector over an already-constructed record (the earlier version tried `OrderBy(t =>
        // t.Item.Name)` after projecting into TileMenuItemResponse/ItemResponse and failed with a
        // client-eval-not-supported error), so the join stays on plain entities until the very end.
        var items = await dbContext.Items.AsNoTracking()
            .Where(i => i.IsSold && i.IsTileDisplay && i.Status == "active")
            .Join(dbContext.ItemBranchSettings.Where(s => s.BranchId == query.BranchId && s.Status == "active"),
                i => i.Id, s => s.ItemId, (i, s) => new { Item = i, Setting = s })
            .OrderBy(t => t.Item.Name)
            .Select(t => new TileMenuItemResponse(
                new ItemResponse(
                    t.Item.Id, t.Item.Code, t.Item.Name, t.Item.Description, t.Item.CategoryId, t.Item.BaseUomId, t.Item.ItemType, t.Item.CostingMethod,
                    t.Item.IsSerialized, t.Item.IsBatchTracked, t.Item.IsSold, t.Item.IsPurchased, t.Item.IsStocked, t.Item.IsTileDisplay, t.Item.AllowNegativeStock,
                    t.Item.SalesAccountId, t.Item.PurchaseAccountId, t.Item.InventoryAccountId, t.Item.CogsAccountId,
                    t.Item.VatType, t.Item.Status, t.Item.CreatedAt),
                t.Setting.SellingPrice ?? 0))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
