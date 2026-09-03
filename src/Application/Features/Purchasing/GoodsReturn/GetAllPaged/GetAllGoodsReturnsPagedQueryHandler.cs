namespace ZARI.Application.Features.Purchasing.GoodsReturns.GetAllPaged;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReturns.GetAll;
using ZARI.Application.Features.Purchasing.GoodsReturns.Shared;
using ZARI.Domain.Common;

public sealed class GetAllGoodsReturnsPagedQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllGoodsReturnsPagedQuery, Result<PagedResult<GoodsReturnResponse>>>
{
    public async Task<Result<PagedResult<GoodsReturnResponse>>> HandleAsync(GetAllGoodsReturnsPagedQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("GOODS_RETURNS", FormAction.View, cancellationToken))
            return Result.Failure<PagedResult<GoodsReturnResponse>>(Error.Forbidden("GoodsReturn.Forbidden", "You do not have permission to view goods returns."));

        var baseQuery = dbContext.GoodsReturns.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(x => x.ReturnNo.Contains(query.Search));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var returns = await baseQuery
            .OrderByDescending(r => r.ReturnDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(r => r.Supplier)
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<GoodsReturnResponse>(returns.Select(GoodsReturnMapper.ToResponse).ToList(), totalCount, query.Page, query.PageSize));
    }
}
