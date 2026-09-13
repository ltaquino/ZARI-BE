namespace ZARI.Application.Features.Hr.PayrollRuns.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdatePayrollRunCommand(
    Guid Id,
    DateTimeOffset PayPeriodStart,
    DateTimeOffset PayPeriodEnd,
    DateTimeOffset PayDate) : ICommand;
