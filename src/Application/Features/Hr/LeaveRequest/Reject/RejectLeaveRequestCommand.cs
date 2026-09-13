namespace ZARI.Application.Features.Hr.LeaveRequests.Reject;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.LeaveRequests.Get;
using ZARI.Domain.Common;

public sealed record RejectLeaveRequestCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<LeaveRequestResponse>>;
