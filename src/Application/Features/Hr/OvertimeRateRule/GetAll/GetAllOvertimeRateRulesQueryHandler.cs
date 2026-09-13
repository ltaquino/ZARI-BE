namespace ZARI.Application.Features.Hr.OvertimeRateRules.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.OvertimeRateRules.Get;
using ZARI.Domain.Common;

public sealed class GetAllOvertimeRateRulesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllOvertimeRateRulesQuery, Result<List<OvertimeRateRuleResponse>>>
{
    public async Task<Result<List<OvertimeRateRuleResponse>>> HandleAsync(GetAllOvertimeRateRulesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("OVERTIME_RATE_RULES", FormAction.View, cancellationToken))
            return Result.Failure<List<OvertimeRateRuleResponse>>(Error.Forbidden("OvertimeRateRule.Forbidden", "You do not have permission to view overtime rate rules."));

        var items = await dbContext.OvertimeRateRules.AsNoTracking()
            .OrderBy(o => o.OtType).ThenByDescending(o => o.EffectiveDate)
            .Select(o => new OvertimeRateRuleResponse(o.Id, o.OtType, o.Multiplier, o.EffectiveDate, o.Status, o.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
