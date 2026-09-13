namespace ZARI.Application.Features.Hr.LeaveRequests.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.LeaveRequests.Get;
using ZARI.Domain.Common;

public sealed record CreateLeaveRequestCommand(
    Guid EmployeeId,
    Guid LeaveTypeId,
    DateTimeOffset DateFrom,
    DateTimeOffset DateTo,
    string? Reason,
    string? AttachmentUrl) : ICommand<Result<LeaveRequestResponse>>;
