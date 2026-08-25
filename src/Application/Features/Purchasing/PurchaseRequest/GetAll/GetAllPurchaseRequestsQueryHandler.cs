namespace ZARI.Application.Features.Purchasing.PurchaseRequests.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseRequests.Shared;
using ZARI.Domain.Common;

public sealed class GetAllPurchaseRequestsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllPurchaseRequestsQuery, Result<List<PurchaseRequestResponse>>>
{
    public async Task<Result<List<PurchaseRequestResponse>>> HandleAsync(GetAllPurchaseRequestsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("PURCHASE_REQUESTS", FormAction.View, cancellationToken))
            return Result.Failure<List<PurchaseRequestResponse>>(Error.Forbidden("PurchaseRequest.Forbidden", "You do not have permission to view purchase requests."));

        var requests = await dbContext.PurchaseRequests
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .OrderByDescending(r => r.RequestDate)
            .ToListAsync(cancellationToken);

        return Result.Success(requests.Select(PurchaseRequestMapper.ToResponse).ToList());
    }
}
