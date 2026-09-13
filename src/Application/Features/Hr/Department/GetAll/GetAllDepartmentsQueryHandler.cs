namespace ZARI.Application.Features.Hr.Departments.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.Departments.Get;
using ZARI.Domain.Common;

public sealed class GetAllDepartmentsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllDepartmentsQuery, Result<List<DepartmentResponse>>>
{
    public async Task<Result<List<DepartmentResponse>>> HandleAsync(GetAllDepartmentsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("DEPARTMENTS", FormAction.View, cancellationToken))
            return Result.Failure<List<DepartmentResponse>>(Error.Forbidden("Department.Forbidden", "You do not have permission to view departments."));

        var items = await dbContext.Departments.AsNoTracking()
            .OrderBy(d => d.Code)
            .Select(d => new DepartmentResponse(d.Id, d.Code, d.Name, d.Status, d.HeadEmployeeId, d.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
