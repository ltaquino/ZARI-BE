namespace ZARI.Application.Features.Hr.Employees.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteEmployeeCommand(Guid Id) : ICommand;
