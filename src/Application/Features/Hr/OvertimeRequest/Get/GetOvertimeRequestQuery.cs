namespace ZARI.Application.Features.Hr.OvertimeRequests.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetOvertimeRequestQuery(Guid Id) : IQuery<Result<OvertimeRequestResponse>>;

public sealed record OvertimeRequestResponse(
    Guid Id,
    string RequestNo,
    Guid EmployeeId,
    DateTimeOffset Date,
    DateTimeOffset PlannedTimeFrom,
    DateTimeOffset PlannedTimeTo,
    string OtType,
    string? Reason,
    string Status,
    DateTimeOffset CreatedAt);
