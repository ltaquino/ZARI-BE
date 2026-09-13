namespace ZARI.Application.Features.Hr.PagIbigContributionBrackets.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetPagIbigContributionBracketQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetPagIbigContributionBracketQuery, Result<PagIbigContributionBracketResponse>>
{
    public async Task<Result<PagIbigContributionBracketResponse>> HandleAsync(GetPagIbigContributionBracketQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("PAGIBIG_BRACKETS", FormAction.View, cancellationToken))
            return Result.Failure<PagIbigContributionBracketResponse>(Error.Forbidden("PagIbigContributionBracket.Forbidden", "You do not have permission to view Pag-IBIG contribution brackets."));

        var bracket = await dbContext.PagIbigContributionBrackets
            .Where(s => s.Id == query.Id)
            .Select(s => new PagIbigContributionBracketResponse(s.Id, s.SalaryFrom, s.SalaryTo, s.EmployeeShare, s.EmployerShare, s.EffectiveDate, s.Status, s.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (bracket is null)
            return Result.Failure<PagIbigContributionBracketResponse>(Error.NotFound("PagIbigContributionBracket.NotFound", $"Pag-IBIG contribution bracket with ID '{query.Id}' was not found."));

        return Result.Success(bracket);
    }
}
