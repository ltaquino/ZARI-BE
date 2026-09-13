namespace ZARI.Application.Features.Hr.SssContributionBrackets.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.SssContributionBrackets.Get;
using ZARI.Domain.Common;

public sealed class GetAllSssContributionBracketsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllSssContributionBracketsQuery, Result<List<SssContributionBracketResponse>>>
{
    public async Task<Result<List<SssContributionBracketResponse>>> HandleAsync(GetAllSssContributionBracketsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("SSS_CONTRIBUTION_BRACKETS", FormAction.View, cancellationToken))
            return Result.Failure<List<SssContributionBracketResponse>>(Error.Forbidden("SssContributionBracket.Forbidden", "You do not have permission to view SSS contribution brackets."));

        var items = await dbContext.SssContributionBrackets.AsNoTracking()
            .OrderByDescending(s => s.EffectiveDate).ThenBy(s => s.SalaryFrom)
            .Select(s => new SssContributionBracketResponse(s.Id, s.SalaryFrom, s.SalaryTo, s.EmployeeShare, s.EmployerShare, s.EffectiveDate, s.Status, s.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
