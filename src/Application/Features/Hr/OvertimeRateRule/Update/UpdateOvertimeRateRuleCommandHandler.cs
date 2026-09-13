namespace ZARI.Application.Features.Hr.OvertimeRateRules.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateOvertimeRateRuleCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<UpdateOvertimeRateRuleCommand>
{
    public async Task<Result> HandleAsync(UpdateOvertimeRateRuleCommand command, CancellationToken cancellationToken = default)
    {
        var rule = await dbContext.OvertimeRateRules.FindAsync([command.Id], cancellationToken);
        if (rule is null)
            return Result.Failure(Error.NotFound("OvertimeRateRule.NotFound", $"Overtime rate rule with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("OVERTIME_RATE_RULES", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("OvertimeRateRule.Forbidden", "You do not have permission to update overtime rate rules."));

        var duplicateExists = await dbContext.OvertimeRateRules
            .AnyAsync(o => o.Id != command.Id && o.OtType == command.OtType && o.EffectiveDate == command.EffectiveDate, cancellationToken);
        if (duplicateExists)
            return Result.Failure(Error.Conflict("OvertimeRateRule.Duplicate", $"An overtime rate rule for '{command.OtType}' effective on {command.EffectiveDate:d} already exists."));

        rule.OtType = command.OtType;
        rule.Multiplier = command.Multiplier;
        rule.EffectiveDate = command.EffectiveDate;
        rule.Status = command.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
