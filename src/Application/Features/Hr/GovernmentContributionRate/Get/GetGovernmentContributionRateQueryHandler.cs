namespace ZARI.Application.Features.Hr.GovernmentContributionRates.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetGovernmentContributionRateQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetGovernmentContributionRateQuery, Result<GovernmentContributionRateResponse>>
{
    public async Task<Result<GovernmentContributionRateResponse>> HandleAsync(GetGovernmentContributionRateQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("GOVT_CONTRIBUTION_RATES", FormAction.View, cancellationToken))
            return Result.Failure<GovernmentContributionRateResponse>(Error.Forbidden("GovernmentContributionRate.Forbidden", "You do not have permission to view government contribution rates."));

        var rate = await dbContext.GovernmentContributionRates
            .Where(g => g.Id == query.Id)
            .Select(g => new GovernmentContributionRateResponse(g.Id, g.ContributionType, g.SalaryFrom, g.SalaryTo, g.EmployeeSharePct, g.EmployerSharePct, g.MaxContribution, g.EffectiveDate, g.Status, g.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (rate is null)
            return Result.Failure<GovernmentContributionRateResponse>(Error.NotFound("GovernmentContributionRate.NotFound", $"Government contribution rate with ID '{query.Id}' was not found."));

        return Result.Success(rate);
    }
}
