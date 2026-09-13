namespace ZARI.Application.Features.Hr.PagIbigContributionBrackets.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.PagIbigContributionBrackets.Get;
using ZARI.Domain.Common;

public sealed class GetAllPagIbigContributionBracketsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllPagIbigContributionBracketsQuery, Result<List<PagIbigContributionBracketResponse>>>
{
    public async Task<Result<List<PagIbigContributionBracketResponse>>> HandleAsync(GetAllPagIbigContributionBracketsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("PAGIBIG_BRACKETS", FormAction.View, cancellationToken))
            return Result.Failure<List<PagIbigContributionBracketResponse>>(Error.Forbidden("PagIbigContributionBracket.Forbidden", "You do not have permission to view Pag-IBIG contribution brackets."));

        var items = await dbContext.PagIbigContributionBrackets.AsNoTracking()
            .OrderByDescending(s => s.EffectiveDate).ThenBy(s => s.SalaryFrom)
            .Select(s => new PagIbigContributionBracketResponse(s.Id, s.SalaryFrom, s.SalaryTo, s.EmployeeShare, s.EmployerShare, s.EffectiveDate, s.Status, s.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
