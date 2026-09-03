namespace ZARI.Application.Features.Sales.SalesReturns.GetAllPaged;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesReturns.GetAll;
using ZARI.Application.Features.Sales.SalesReturns.Shared;
using ZARI.Domain.Common;

public sealed class GetAllSalesReturnsPagedQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllSalesReturnsPagedQuery, Result<PagedResult<SalesReturnResponse>>>
{
    public async Task<Result<PagedResult<SalesReturnResponse>>> HandleAsync(GetAllSalesReturnsPagedQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("SALES_RETURNS", FormAction.View, cancellationToken))
            return Result.Failure<PagedResult<SalesReturnResponse>>(Error.Forbidden("SalesReturn.Forbidden", "You do not have permission to view sales returns."));

        var baseQuery = dbContext.SalesReturns.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(x => x.ReturnNo.Contains(query.Search));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var returns = await baseQuery
            .OrderByDescending(r => r.ReturnDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(r => r.Customer)
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<SalesReturnResponse>(returns.Select(SalesReturnMapper.ToResponse).ToList(), totalCount, query.Page, query.PageSize));
    }
}
