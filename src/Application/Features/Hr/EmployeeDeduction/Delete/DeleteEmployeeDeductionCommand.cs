namespace ZARI.Application.Features.Hr.EmployeeDeductions.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteEmployeeDeductionCommand(Guid Id) : ICommand;
