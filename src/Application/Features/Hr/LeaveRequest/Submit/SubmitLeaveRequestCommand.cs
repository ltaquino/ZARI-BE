namespace ZARI.Application.Features.Hr.LeaveRequests.Submit;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.LeaveRequests.Get;
using ZARI.Domain.Common;

public sealed record SubmitLeaveRequestCommand(Guid Id, string RequestedBy) : ICommand<Result<LeaveRequestResponse>>;
