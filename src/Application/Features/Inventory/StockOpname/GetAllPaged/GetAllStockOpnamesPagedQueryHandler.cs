namespace ZARI.Application.Features.Inventory.StockOpnames.GetAllPaged;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockOpnames.GetAll;
using ZARI.Application.Features.Inventory.StockOpnames.Shared;
using ZARI.Domain.Common;

public sealed class GetAllStockOpnamesPagedQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllStockOpnamesPagedQuery, Result<PagedResult<StockOpnameResponse>>>
{
    public async Task<Result<PagedResult<StockOpnameResponse>>> HandleAsync(GetAllStockOpnamesPagedQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("STOCK_OPNAMES", FormAction.View, cancellationToken))
            return Result.Failure<PagedResult<StockOpnameResponse>>(Error.Forbidden("StockOpname.Forbidden", "You do not have permission to view stock opnames."));

        var baseQuery = dbContext.StockOpnames.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(x => x.OpnameNo.Contains(query.Search));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var opnames = await baseQuery
            .OrderByDescending(o => o.CountDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(o => o.Lines).ThenInclude(l => l.Item).ThenInclude(i => i.BaseUom)
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<StockOpnameResponse>(opnames.Select(StockOpnameMapper.ToResponse).ToList(), totalCount, query.Page, query.PageSize));
    }
}
