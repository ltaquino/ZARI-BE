namespace ZARI.Application.Features.Hr.PayrollRuns.Approve;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.PayrollRuns.Get;
using ZARI.Domain.Common;

public sealed record ApprovePayrollRunCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<PayrollRunResponse>>;
