namespace ZARI.Application.Features.Hr.SssContributionBrackets.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetSssContributionBracketQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetSssContributionBracketQuery, Result<SssContributionBracketResponse>>
{
    public async Task<Result<SssContributionBracketResponse>> HandleAsync(GetSssContributionBracketQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("SSS_CONTRIBUTION_BRACKETS", FormAction.View, cancellationToken))
            return Result.Failure<SssContributionBracketResponse>(Error.Forbidden("SssContributionBracket.Forbidden", "You do not have permission to view SSS contribution brackets."));

        var bracket = await dbContext.SssContributionBrackets
            .Where(s => s.Id == query.Id)
            .Select(s => new SssContributionBracketResponse(s.Id, s.SalaryFrom, s.SalaryTo, s.EmployeeShare, s.EmployerShare, s.EffectiveDate, s.Status, s.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (bracket is null)
            return Result.Failure<SssContributionBracketResponse>(Error.NotFound("SssContributionBracket.NotFound", $"SSS contribution bracket with ID '{query.Id}' was not found."));

        return Result.Success(bracket);
    }
}
