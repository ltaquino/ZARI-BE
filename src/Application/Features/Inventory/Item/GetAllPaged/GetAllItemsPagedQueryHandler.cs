namespace ZARI.Application.Features.Inventory.Items.GetAllPaged;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Items.Get;
using ZARI.Domain.Common;

public sealed class GetAllItemsPagedQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllItemsPagedQuery, Result<PagedResult<ItemResponse>>>
{
    public async Task<Result<PagedResult<ItemResponse>>> HandleAsync(GetAllItemsPagedQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("ITEMS", FormAction.View, cancellationToken))
            return Result.Failure<PagedResult<ItemResponse>>(Error.Forbidden("Item.Forbidden", "You do not have permission to view items."));

        var baseQuery = dbContext.Items.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(i => i.Code.Contains(query.Search) || i.Name.Contains(query.Search));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .OrderBy(i => i.Code)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(i => new ItemResponse(
                i.Id, i.Code, i.Name, i.Description, i.CategoryId, i.BaseUomId, i.ItemType, i.CostingMethod,
                i.IsSerialized, i.IsBatchTracked, i.IsSold, i.IsPurchased, i.IsStocked, i.IsTileDisplay,
                i.SalesAccountId, i.PurchaseAccountId, i.InventoryAccountId, i.CogsAccountId,
                i.VatType, i.Status, i.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<ItemResponse>(items, totalCount, query.Page, query.PageSize));
    }
}
