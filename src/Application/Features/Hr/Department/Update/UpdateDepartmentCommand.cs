namespace ZARI.Application.Features.Hr.Departments.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateDepartmentCommand(Guid Id, string Code, string Name, string Status, Guid? HeadEmployeeId) : ICommand;
