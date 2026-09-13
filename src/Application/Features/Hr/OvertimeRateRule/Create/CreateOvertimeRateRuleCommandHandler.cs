namespace ZARI.Application.Features.Hr.OvertimeRateRules.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.OvertimeRateRules.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateOvertimeRateRuleCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<CreateOvertimeRateRuleCommand, Result<OvertimeRateRuleResponse>>
{
    public async Task<Result<OvertimeRateRuleResponse>> HandleAsync(CreateOvertimeRateRuleCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("OVERTIME_RATE_RULES", FormAction.Create, cancellationToken))
            return Result.Failure<OvertimeRateRuleResponse>(Error.Forbidden("OvertimeRateRule.Forbidden", "You do not have permission to create overtime rate rules."));

        var duplicateExists = await dbContext.OvertimeRateRules
            .AnyAsync(o => o.OtType == command.OtType && o.EffectiveDate == command.EffectiveDate, cancellationToken);
        if (duplicateExists)
            return Result.Failure<OvertimeRateRuleResponse>(Error.Conflict("OvertimeRateRule.Duplicate", $"An overtime rate rule for '{command.OtType}' effective on {command.EffectiveDate:d} already exists."));

        var rule = new OvertimeRateRule
        {
            OtType = command.OtType,
            Multiplier = command.Multiplier,
            EffectiveDate = command.EffectiveDate,
            Status = command.Status
        };

        dbContext.OvertimeRateRules.Add(rule);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new OvertimeRateRuleResponse(rule.Id, rule.OtType, rule.Multiplier, rule.EffectiveDate, rule.Status, rule.CreatedAt);
        return Result.Success(response);
    }
}
