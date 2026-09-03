namespace ZARI.Application.Features.Purchasing.PurchaseRequests.GetAllPaged;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseRequests.GetAll;
using ZARI.Application.Features.Purchasing.PurchaseRequests.Shared;
using ZARI.Domain.Common;

public sealed class GetAllPurchaseRequestsPagedQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllPurchaseRequestsPagedQuery, Result<PagedResult<PurchaseRequestResponse>>>
{
    public async Task<Result<PagedResult<PurchaseRequestResponse>>> HandleAsync(GetAllPurchaseRequestsPagedQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("PURCHASE_REQUESTS", FormAction.View, cancellationToken))
            return Result.Failure<PagedResult<PurchaseRequestResponse>>(Error.Forbidden("PurchaseRequest.Forbidden", "You do not have permission to view purchase requests."));

        var baseQuery = dbContext.PurchaseRequests.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(x => x.RequestNo.Contains(query.Search));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var requests = await baseQuery
            .OrderByDescending(r => r.RequestDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<PurchaseRequestResponse>(requests.Select(PurchaseRequestMapper.ToResponse).ToList(), totalCount, query.Page, query.PageSize));
    }
}
