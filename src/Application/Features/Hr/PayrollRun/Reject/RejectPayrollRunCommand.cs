namespace ZARI.Application.Features.Hr.PayrollRuns.Reject;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.PayrollRuns.Get;
using ZARI.Domain.Common;

public sealed record RejectPayrollRunCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<PayrollRunResponse>>;
