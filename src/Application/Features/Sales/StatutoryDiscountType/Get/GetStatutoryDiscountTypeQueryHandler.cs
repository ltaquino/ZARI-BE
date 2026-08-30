namespace ZARI.Application.Features.Sales.StatutoryDiscountTypes.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetStatutoryDiscountTypeQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetStatutoryDiscountTypeQuery, Result<StatutoryDiscountTypeResponse>>
{
    public async Task<Result<StatutoryDiscountTypeResponse>> HandleAsync(GetStatutoryDiscountTypeQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("STATUTORY_DISCOUNT_TYPES", FormAction.View, cancellationToken))
            return Result.Failure<StatutoryDiscountTypeResponse>(Error.Forbidden("StatutoryDiscountType.Forbidden", "You do not have permission to view statutory discount types."));

        var type = await dbContext.StatutoryDiscountTypes
            .Where(t => t.Id == query.Id)
            .Select(t => new StatutoryDiscountTypeResponse(t.Id, t.Code, t.Name, t.DiscountPct, t.IsVatExempt, t.RequiredIdLabel, t.Status, t.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (type is null)
            return Result.Failure<StatutoryDiscountTypeResponse>(Error.NotFound("StatutoryDiscountType.NotFound", $"Statutory discount type with ID '{query.Id}' was not found."));

        return Result.Success(type);
    }
}
