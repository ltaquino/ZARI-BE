namespace ZARI.Application.Features.Hr.Departments.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteDepartmentCommand(Guid Id) : ICommand;
