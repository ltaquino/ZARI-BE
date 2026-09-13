namespace ZARI.Application.Features.Hr.Departments.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetDepartmentQuery(Guid Id) : IQuery<Result<DepartmentResponse>>;

public sealed record DepartmentResponse(
    Guid Id,
    string Code,
    string Name,
    string Status,
    Guid? HeadEmployeeId,
    DateTimeOffset CreatedAt);
