namespace ZARI.Application.Features.Accounting.FiscalYears.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.FiscalYears.Get;
using ZARI.Domain.Common;

public sealed class GetAllFiscalYearsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllFiscalYearsQuery, Result<List<FiscalYearResponse>>>
{
    public async Task<Result<List<FiscalYearResponse>>> HandleAsync(GetAllFiscalYearsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("FISCAL_YEARS", FormAction.View, cancellationToken))
            return Result.Failure<List<FiscalYearResponse>>(Error.Forbidden("FiscalYear.Forbidden", "You do not have permission to view fiscal years."));

        var items = await dbContext.FiscalYears.AsNoTracking()
            .OrderByDescending(f => f.StartDate)
            .Select(f => new FiscalYearResponse(f.Id, f.YearName, f.StartDate, f.EndDate, f.Status, f.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
