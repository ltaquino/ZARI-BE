namespace ZARI.Application.Features.Hr.DailyTimeRecords.KioskClock;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record ClockDtrKioskCommand(string ScannedCode) : ICommand<Result<DtrKioskClockResponse>>;

public sealed record DtrKioskClockResponse(
    Guid EmployeeId,
    string EmployeeNo,
    string EmployeeName,
    string ClockAction,
    DateTimeOffset Timestamp,
    Guid DailyTimeRecordId,
    int? LateMinutes,
    int? UndertimeMinutes);
