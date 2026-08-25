namespace ZARI.Application.Features.Purchasing.PurchaseRequests.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseRequests.GetAll;
using ZARI.Application.Features.Purchasing.PurchaseRequests.Shared;
using ZARI.Domain.Common;

public sealed class GetPurchaseRequestQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetPurchaseRequestQuery, Result<PurchaseRequestResponse>>
{
    public async Task<Result<PurchaseRequestResponse>> HandleAsync(GetPurchaseRequestQuery query, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.PurchaseRequests
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == query.Id, cancellationToken);

        if (request is null)
            return Result.Failure<PurchaseRequestResponse>(Error.NotFound("PurchaseRequest.NotFound", $"Purchase request with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("PURCHASE_REQUESTS", FormAction.View, request.BranchId, cancellationToken))
            return Result.Failure<PurchaseRequestResponse>(Error.Forbidden("PurchaseRequest.Forbidden", "You do not have permission to view purchase requests for this branch."));

        return Result.Success(PurchaseRequestMapper.ToResponse(request));
    }
}
