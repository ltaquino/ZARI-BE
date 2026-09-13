namespace ZARI.Application.Features.Hr.EmployeeAllowances.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetEmployeeAllowanceQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetEmployeeAllowanceQuery, Result<EmployeeAllowanceResponse>>
{
    public async Task<Result<EmployeeAllowanceResponse>> HandleAsync(GetEmployeeAllowanceQuery query, CancellationToken cancellationToken = default)
    {
        var row = await dbContext.EmployeeAllowances
            .Where(a => a.Id == query.Id)
            .Select(a => new { a.Id, a.EmployeeId, a.Code, a.Name, a.Amount, a.EffectiveDate, a.EndDate, a.Status, a.CreatedAt, a.Employee.BranchId })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
            return Result.Failure<EmployeeAllowanceResponse>(Error.NotFound("EmployeeAllowance.NotFound", $"Employee allowance with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("EMPLOYEE_ALLOWANCES", FormAction.View, row.BranchId, cancellationToken))
            return Result.Failure<EmployeeAllowanceResponse>(Error.Forbidden("EmployeeAllowance.Forbidden", "You do not have permission to view employee allowances for this branch."));

        var response = new EmployeeAllowanceResponse(row.Id, row.EmployeeId, row.Code, row.Name, row.Amount, row.EffectiveDate, row.EndDate, row.Status, row.CreatedAt);
        return Result.Success(response);
    }
}
