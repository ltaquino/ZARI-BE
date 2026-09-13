namespace ZARI.Application.Features.Hr.LeaveRequests.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetLeaveRequestQuery(Guid Id) : IQuery<Result<LeaveRequestResponse>>;

public sealed record LeaveRequestResponse(
    Guid Id,
    string RequestNo,
    Guid EmployeeId,
    Guid LeaveTypeId,
    DateTimeOffset DateFrom,
    DateTimeOffset DateTo,
    decimal Days,
    string? Reason,
    string? AttachmentUrl,
    string Status,
    DateTimeOffset CreatedAt);
