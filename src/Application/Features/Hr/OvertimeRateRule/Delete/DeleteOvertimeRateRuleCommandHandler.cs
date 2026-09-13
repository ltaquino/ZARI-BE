namespace ZARI.Application.Features.Hr.OvertimeRateRules.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteOvertimeRateRuleCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<DeleteOvertimeRateRuleCommand>
{
    public async Task<Result> HandleAsync(DeleteOvertimeRateRuleCommand command, CancellationToken cancellationToken = default)
    {
        var rule = await dbContext.OvertimeRateRules.FindAsync([command.Id], cancellationToken);
        if (rule is null)
            return Result.Failure(Error.NotFound("OvertimeRateRule.NotFound", $"Overtime rate rule with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("OVERTIME_RATE_RULES", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("OvertimeRateRule.Forbidden", "You do not have permission to delete overtime rate rules."));

        dbContext.OvertimeRateRules.Remove(rule);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
