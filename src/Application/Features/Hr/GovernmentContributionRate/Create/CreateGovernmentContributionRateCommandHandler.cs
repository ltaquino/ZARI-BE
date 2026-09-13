namespace ZARI.Application.Features.Hr.GovernmentContributionRates.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.GovernmentContributionRates.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateGovernmentContributionRateCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<CreateGovernmentContributionRateCommand, Result<GovernmentContributionRateResponse>>
{
    public async Task<Result<GovernmentContributionRateResponse>> HandleAsync(CreateGovernmentContributionRateCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("GOVT_CONTRIBUTION_RATES", FormAction.Create, cancellationToken))
            return Result.Failure<GovernmentContributionRateResponse>(Error.Forbidden("GovernmentContributionRate.Forbidden", "You do not have permission to create government contribution rates."));

        var duplicateExists = await dbContext.GovernmentContributionRates
            .AnyAsync(g => g.ContributionType == command.ContributionType && g.EffectiveDate == command.EffectiveDate && g.SalaryFrom == command.SalaryFrom, cancellationToken);
        if (duplicateExists)
            return Result.Failure<GovernmentContributionRateResponse>(Error.Conflict("GovernmentContributionRate.Duplicate", $"A {command.ContributionType} rate starting at {command.SalaryFrom:N2} effective {command.EffectiveDate:d} already exists."));

        var rate = new GovernmentContributionRate
        {
            ContributionType = command.ContributionType,
            SalaryFrom = command.SalaryFrom,
            SalaryTo = command.SalaryTo,
            EmployeeSharePct = command.EmployeeSharePct,
            EmployerSharePct = command.EmployerSharePct,
            MaxContribution = command.MaxContribution,
            EffectiveDate = command.EffectiveDate,
            Status = command.Status
        };

        dbContext.GovernmentContributionRates.Add(rate);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new GovernmentContributionRateResponse(rate.Id, rate.ContributionType, rate.SalaryFrom, rate.SalaryTo, rate.EmployeeSharePct, rate.EmployerSharePct, rate.MaxContribution, rate.EffectiveDate, rate.Status, rate.CreatedAt);
        return Result.Success(response);
    }
}
