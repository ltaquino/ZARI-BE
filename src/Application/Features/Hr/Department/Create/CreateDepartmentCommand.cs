namespace ZARI.Application.Features.Hr.Departments.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.Departments.Get;
using ZARI.Domain.Common;

public sealed record CreateDepartmentCommand(string Code, string Name, string Status, Guid? HeadEmployeeId)
    : ICommand<Result<DepartmentResponse>>;
