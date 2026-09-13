namespace ZARI.Application.Features.Hr.GovernmentContributionRates.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteGovernmentContributionRateCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<DeleteGovernmentContributionRateCommand>
{
    public async Task<Result> HandleAsync(DeleteGovernmentContributionRateCommand command, CancellationToken cancellationToken = default)
    {
        var rate = await dbContext.GovernmentContributionRates.FindAsync([command.Id], cancellationToken);
        if (rate is null)
            return Result.Failure(Error.NotFound("GovernmentContributionRate.NotFound", $"Government contribution rate with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("GOVT_CONTRIBUTION_RATES", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("GovernmentContributionRate.Forbidden", "You do not have permission to delete government contribution rates."));

        dbContext.GovernmentContributionRates.Remove(rate);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
