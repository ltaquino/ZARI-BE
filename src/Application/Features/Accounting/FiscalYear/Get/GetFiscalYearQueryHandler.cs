namespace ZARI.Application.Features.Accounting.FiscalYears.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetFiscalYearQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetFiscalYearQuery, Result<FiscalYearResponse>>
{
    public async Task<Result<FiscalYearResponse>> HandleAsync(GetFiscalYearQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("FISCAL_YEARS", FormAction.View, cancellationToken))
            return Result.Failure<FiscalYearResponse>(Error.Forbidden("FiscalYear.Forbidden", "You do not have permission to view fiscal years."));

        var fiscalYear = await dbContext.FiscalYears
            .Where(f => f.Id == query.Id)
            .Select(f => new FiscalYearResponse(f.Id, f.YearName, f.StartDate, f.EndDate, f.Status, f.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (fiscalYear is null)
            return Result.Failure<FiscalYearResponse>(Error.NotFound("FiscalYear.NotFound", $"Fiscal year with ID '{query.Id}' was not found."));

        return Result.Success(fiscalYear);
    }
}
