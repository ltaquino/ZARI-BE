namespace ZARI.Application.Features.Hr.DailyTimeRecords.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetDailyTimeRecordQuery(Guid Id) : IQuery<Result<DailyTimeRecordResponse>>;

public sealed record DailyTimeRecordResponse(
    Guid Id,
    Guid EmployeeId,
    DateTimeOffset Date,
    DateTimeOffset? TimeIn,
    DateTimeOffset? TimeOut,
    DateTimeOffset? BreakOutTime,
    DateTimeOffset? BreakInTime,
    string Source,
    string? Remarks,
    int? LateMinutes,
    int? UndertimeMinutes,
    bool IsAbsent,
    DateTimeOffset CreatedAt);
