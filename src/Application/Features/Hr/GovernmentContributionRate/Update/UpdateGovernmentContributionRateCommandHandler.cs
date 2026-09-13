namespace ZARI.Application.Features.Hr.GovernmentContributionRates.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateGovernmentContributionRateCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<UpdateGovernmentContributionRateCommand>
{
    public async Task<Result> HandleAsync(UpdateGovernmentContributionRateCommand command, CancellationToken cancellationToken = default)
    {
        var rate = await dbContext.GovernmentContributionRates.FindAsync([command.Id], cancellationToken);
        if (rate is null)
            return Result.Failure(Error.NotFound("GovernmentContributionRate.NotFound", $"Government contribution rate with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("GOVT_CONTRIBUTION_RATES", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("GovernmentContributionRate.Forbidden", "You do not have permission to update government contribution rates."));

        var duplicateExists = await dbContext.GovernmentContributionRates
            .AnyAsync(g => g.Id != command.Id && g.ContributionType == command.ContributionType && g.EffectiveDate == command.EffectiveDate && g.SalaryFrom == command.SalaryFrom, cancellationToken);
        if (duplicateExists)
            return Result.Failure(Error.Conflict("GovernmentContributionRate.Duplicate", $"A {command.ContributionType} rate starting at {command.SalaryFrom:N2} effective {command.EffectiveDate:d} already exists."));

        rate.ContributionType = command.ContributionType;
        rate.SalaryFrom = command.SalaryFrom;
        rate.SalaryTo = command.SalaryTo;
        rate.EmployeeSharePct = command.EmployeeSharePct;
        rate.EmployerSharePct = command.EmployerSharePct;
        rate.MaxContribution = command.MaxContribution;
        rate.EffectiveDate = command.EffectiveDate;
        rate.Status = command.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
