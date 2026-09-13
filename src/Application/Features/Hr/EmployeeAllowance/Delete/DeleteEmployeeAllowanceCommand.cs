namespace ZARI.Application.Features.Hr.EmployeeAllowances.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteEmployeeAllowanceCommand(Guid Id) : ICommand;
