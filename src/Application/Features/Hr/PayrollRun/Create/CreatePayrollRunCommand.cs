namespace ZARI.Application.Features.Hr.PayrollRuns.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.PayrollRuns.Get;
using ZARI.Domain.Common;

public sealed record CreatePayrollRunCommand(
    string BranchId,
    DateTimeOffset PayPeriodStart,
    DateTimeOffset PayPeriodEnd,
    DateTimeOffset PayDate) : ICommand<Result<PayrollRunResponse>>;
