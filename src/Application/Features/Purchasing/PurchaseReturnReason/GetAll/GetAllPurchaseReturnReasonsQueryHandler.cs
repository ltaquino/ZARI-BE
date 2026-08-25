namespace ZARI.Application.Features.Purchasing.PurchaseReturnReasons.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseReturnReasons.Get;
using ZARI.Domain.Common;

public sealed class GetAllPurchaseReturnReasonsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllPurchaseReturnReasonsQuery, Result<List<PurchaseReturnReasonResponse>>>
{
    public async Task<Result<List<PurchaseReturnReasonResponse>>> HandleAsync(GetAllPurchaseReturnReasonsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("PURCHASE_RETURN_REASONS", FormAction.View, cancellationToken))
            return Result.Failure<List<PurchaseReturnReasonResponse>>(Error.Forbidden("PurchaseReturnReason.Forbidden", "You do not have permission to view purchase return reasons."));

        var items = await dbContext.PurchaseReturnReasons
            .OrderBy(r => r.Code)
            .Select(r => new PurchaseReturnReasonResponse(r.Id, r.Code, r.Description, r.Status, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
