namespace ZARI.Application.Features.Hr.LeaveRequests.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateLeaveRequestCommand(
    Guid Id,
    Guid LeaveTypeId,
    DateTimeOffset DateFrom,
    DateTimeOffset DateTo,
    string? Reason,
    string? AttachmentUrl) : ICommand;
