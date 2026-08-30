namespace ZARI.Application.Features.Sales.DiscountRules.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetDiscountRuleQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetDiscountRuleQuery, Result<DiscountRuleResponse>>
{
    public async Task<Result<DiscountRuleResponse>> HandleAsync(GetDiscountRuleQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("DISCOUNT_RULES", FormAction.View, cancellationToken))
            return Result.Failure<DiscountRuleResponse>(Error.Forbidden("DiscountRule.Forbidden", "You do not have permission to view discount rules."));

        var rule = await dbContext.DiscountRules
            .Where(r => r.Id == query.Id)
            .Select(r => new DiscountRuleResponse(r.Id, r.Code, r.Name, r.Scope, r.ItemId, r.ItemCategoryId, r.DiscountType, r.DiscountValue,
                r.MinQty, r.StartDate, r.EndDate, r.BranchId, r.Priority, r.Status, r.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (rule is null)
            return Result.Failure<DiscountRuleResponse>(Error.NotFound("DiscountRule.NotFound", $"Discount rule with ID '{query.Id}' was not found."));

        return Result.Success(rule);
    }
}
