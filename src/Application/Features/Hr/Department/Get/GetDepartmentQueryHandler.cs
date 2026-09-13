namespace ZARI.Application.Features.Hr.Departments.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetDepartmentQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetDepartmentQuery, Result<DepartmentResponse>>
{
    public async Task<Result<DepartmentResponse>> HandleAsync(GetDepartmentQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("DEPARTMENTS", FormAction.View, cancellationToken))
            return Result.Failure<DepartmentResponse>(Error.Forbidden("Department.Forbidden", "You do not have permission to view departments."));

        var department = await dbContext.Departments
            .Where(d => d.Id == query.Id)
            .Select(d => new DepartmentResponse(d.Id, d.Code, d.Name, d.Status, d.HeadEmployeeId, d.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (department is null)
            return Result.Failure<DepartmentResponse>(Error.NotFound("Department.NotFound", $"Department with ID '{query.Id}' was not found."));

        return Result.Success(department);
    }
}
