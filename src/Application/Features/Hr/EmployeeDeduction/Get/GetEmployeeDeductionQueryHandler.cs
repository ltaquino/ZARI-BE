namespace ZARI.Application.Features.Hr.EmployeeDeductions.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetEmployeeDeductionQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetEmployeeDeductionQuery, Result<EmployeeDeductionResponse>>
{
    public async Task<Result<EmployeeDeductionResponse>> HandleAsync(GetEmployeeDeductionQuery query, CancellationToken cancellationToken = default)
    {
        var row = await dbContext.EmployeeDeductions
            .Where(d => d.Id == query.Id)
            .Select(d => new { d.Id, d.EmployeeId, d.Code, d.Name, d.Amount, d.EffectiveDate, d.EndDate, d.Status, d.CreatedAt, d.Employee.BranchId })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
            return Result.Failure<EmployeeDeductionResponse>(Error.NotFound("EmployeeDeduction.NotFound", $"Employee deduction with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("EMPLOYEE_DEDUCTIONS", FormAction.View, row.BranchId, cancellationToken))
            return Result.Failure<EmployeeDeductionResponse>(Error.Forbidden("EmployeeDeduction.Forbidden", "You do not have permission to view employee deductions for this branch."));

        var response = new EmployeeDeductionResponse(row.Id, row.EmployeeId, row.Code, row.Name, row.Amount, row.EffectiveDate, row.EndDate, row.Status, row.CreatedAt);
        return Result.Success(response);
    }
}
