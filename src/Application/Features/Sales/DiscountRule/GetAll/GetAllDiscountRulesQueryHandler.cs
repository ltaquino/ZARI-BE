namespace ZARI.Application.Features.Sales.DiscountRules.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.DiscountRules.Get;
using ZARI.Domain.Common;

public sealed class GetAllDiscountRulesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllDiscountRulesQuery, Result<List<DiscountRuleResponse>>>
{
    public async Task<Result<List<DiscountRuleResponse>>> HandleAsync(GetAllDiscountRulesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("DISCOUNT_RULES", FormAction.View, cancellationToken))
            return Result.Failure<List<DiscountRuleResponse>>(Error.Forbidden("DiscountRule.Forbidden", "You do not have permission to view discount rules."));

        var items = await dbContext.DiscountRules.AsNoTracking()
            .OrderBy(r => r.Code)
            .Select(r => new DiscountRuleResponse(r.Id, r.Code, r.Name, r.Scope, r.ItemId, r.ItemCategoryId, r.DiscountType, r.DiscountValue,
                r.MinQty, r.StartDate, r.EndDate, r.BranchId, r.Priority, r.Status, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
