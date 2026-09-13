namespace ZARI.Application.Features.Hr.PayrollRuns.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeletePayrollRunCommand(Guid Id) : ICommand;
