namespace ZARI.Application.Features.Hr.GovernmentContributionRates.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.GovernmentContributionRates.Get;
using ZARI.Domain.Common;

public sealed class GetAllGovernmentContributionRatesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllGovernmentContributionRatesQuery, Result<List<GovernmentContributionRateResponse>>>
{
    public async Task<Result<List<GovernmentContributionRateResponse>>> HandleAsync(GetAllGovernmentContributionRatesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("GOVT_CONTRIBUTION_RATES", FormAction.View, cancellationToken))
            return Result.Failure<List<GovernmentContributionRateResponse>>(Error.Forbidden("GovernmentContributionRate.Forbidden", "You do not have permission to view government contribution rates."));

        var items = await dbContext.GovernmentContributionRates.AsNoTracking()
            .OrderBy(g => g.ContributionType).ThenByDescending(g => g.EffectiveDate).ThenBy(g => g.SalaryFrom)
            .Select(g => new GovernmentContributionRateResponse(g.Id, g.ContributionType, g.SalaryFrom, g.SalaryTo, g.EmployeeSharePct, g.EmployerSharePct, g.MaxContribution, g.EffectiveDate, g.Status, g.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
