namespace ZARI.Application.Features.Sales.StatutoryDiscountTypes.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.StatutoryDiscountTypes.Get;
using ZARI.Domain.Common;

public sealed class GetAllStatutoryDiscountTypesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllStatutoryDiscountTypesQuery, Result<List<StatutoryDiscountTypeResponse>>>
{
    public async Task<Result<List<StatutoryDiscountTypeResponse>>> HandleAsync(GetAllStatutoryDiscountTypesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("STATUTORY_DISCOUNT_TYPES", FormAction.View, cancellationToken))
            return Result.Failure<List<StatutoryDiscountTypeResponse>>(Error.Forbidden("StatutoryDiscountType.Forbidden", "You do not have permission to view statutory discount types."));

        var items = await dbContext.StatutoryDiscountTypes.AsNoTracking()
            .OrderBy(t => t.Code)
            .Select(t => new StatutoryDiscountTypeResponse(t.Id, t.Code, t.Name, t.DiscountPct, t.IsVatExempt, t.RequiredIdLabel, t.Status, t.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
