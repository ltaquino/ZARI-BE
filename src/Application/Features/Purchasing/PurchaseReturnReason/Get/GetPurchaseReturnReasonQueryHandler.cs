namespace ZARI.Application.Features.Purchasing.PurchaseReturnReasons.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetPurchaseReturnReasonQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetPurchaseReturnReasonQuery, Result<PurchaseReturnReasonResponse>>
{
    public async Task<Result<PurchaseReturnReasonResponse>> HandleAsync(GetPurchaseReturnReasonQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("PURCHASE_RETURN_REASONS", FormAction.View, cancellationToken))
            return Result.Failure<PurchaseReturnReasonResponse>(Error.Forbidden("PurchaseReturnReason.Forbidden", "You do not have permission to view purchase return reasons."));

        var reason = await dbContext.PurchaseReturnReasons
            .Where(r => r.Id == query.Id)
            .Select(r => new PurchaseReturnReasonResponse(r.Id, r.Code, r.Description, r.Status, r.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (reason is null)
            return Result.Failure<PurchaseReturnReasonResponse>(Error.NotFound("PurchaseReturnReason.NotFound", $"Purchase return reason with ID '{query.Id}' was not found."));

        return Result.Success(reason);
    }
}
