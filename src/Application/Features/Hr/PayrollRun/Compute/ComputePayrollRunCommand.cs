namespace ZARI.Application.Features.Hr.PayrollRuns.Compute;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.PayrollRuns.Get;
using ZARI.Domain.Common;

public sealed record ComputePayrollRunCommand(Guid Id) : ICommand<Result<PayrollRunResponse>>;
