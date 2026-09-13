namespace ZARI.Application.Features.Hr.PayrollRuns.Submit;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.PayrollRuns.Get;
using ZARI.Domain.Common;

public sealed record SubmitPayrollRunCommand(Guid Id, string RequestedBy) : ICommand<Result<PayrollRunResponse>>;
