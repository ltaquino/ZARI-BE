namespace ZARI.Application.Features.Hr.EmployeeDeductions.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.EmployeeDeductions.Get;
using ZARI.Domain.Common;

public sealed class GetAllEmployeeDeductionsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllEmployeeDeductionsQuery, Result<List<EmployeeDeductionResponse>>>
{
    public async Task<Result<List<EmployeeDeductionResponse>>> HandleAsync(GetAllEmployeeDeductionsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("EMPLOYEE_DEDUCTIONS", FormAction.View, cancellationToken))
            return Result.Failure<List<EmployeeDeductionResponse>>(Error.Forbidden("EmployeeDeduction.Forbidden", "You do not have permission to view employee deductions."));

        var items = await dbContext.EmployeeDeductions.AsNoTracking()
            .OrderByDescending(d => d.EffectiveDate)
            .Select(d => new EmployeeDeductionResponse(d.Id, d.EmployeeId, d.Code, d.Name, d.Amount, d.EffectiveDate, d.EndDate, d.Status, d.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
