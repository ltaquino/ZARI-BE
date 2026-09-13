namespace ZARI.Application.Features.Hr.OvertimeRequests.Reject;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.OvertimeRequests.Get;
using ZARI.Domain.Common;

public sealed record RejectOvertimeRequestCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<OvertimeRequestResponse>>;
