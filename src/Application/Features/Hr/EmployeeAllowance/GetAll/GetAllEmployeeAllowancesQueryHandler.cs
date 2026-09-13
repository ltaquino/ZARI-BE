namespace ZARI.Application.Features.Hr.EmployeeAllowances.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.EmployeeAllowances.Get;
using ZARI.Domain.Common;

public sealed class GetAllEmployeeAllowancesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllEmployeeAllowancesQuery, Result<List<EmployeeAllowanceResponse>>>
{
    public async Task<Result<List<EmployeeAllowanceResponse>>> HandleAsync(GetAllEmployeeAllowancesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("EMPLOYEE_ALLOWANCES", FormAction.View, cancellationToken))
            return Result.Failure<List<EmployeeAllowanceResponse>>(Error.Forbidden("EmployeeAllowance.Forbidden", "You do not have permission to view employee allowances."));

        var items = await dbContext.EmployeeAllowances.AsNoTracking()
            .OrderByDescending(a => a.EffectiveDate)
            .Select(a => new EmployeeAllowanceResponse(a.Id, a.EmployeeId, a.Code, a.Name, a.Amount, a.EffectiveDate, a.EndDate, a.Status, a.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
