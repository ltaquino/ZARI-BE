namespace ZARI.Application.Features.Hr.OvertimeRateRules.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetOvertimeRateRuleQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetOvertimeRateRuleQuery, Result<OvertimeRateRuleResponse>>
{
    public async Task<Result<OvertimeRateRuleResponse>> HandleAsync(GetOvertimeRateRuleQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("OVERTIME_RATE_RULES", FormAction.View, cancellationToken))
            return Result.Failure<OvertimeRateRuleResponse>(Error.Forbidden("OvertimeRateRule.Forbidden", "You do not have permission to view overtime rate rules."));

        var rule = await dbContext.OvertimeRateRules
            .Where(o => o.Id == query.Id)
            .Select(o => new OvertimeRateRuleResponse(o.Id, o.OtType, o.Multiplier, o.EffectiveDate, o.Status, o.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (rule is null)
            return Result.Failure<OvertimeRateRuleResponse>(Error.NotFound("OvertimeRateRule.NotFound", $"Overtime rate rule with ID '{query.Id}' was not found."));

        return Result.Success(rule);
    }
}
