namespace ZARI.Application.Features.Hr.LeaveRequests.Approve;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.LeaveRequests.Get;
using ZARI.Domain.Common;

public sealed record ApproveLeaveRequestCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<LeaveRequestResponse>>;
