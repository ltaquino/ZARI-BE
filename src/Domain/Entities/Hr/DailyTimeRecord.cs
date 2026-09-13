namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Stores only the raw punch data — LateMinutes/UndertimeMinutes/IsAbsent are deliberately NOT
/// columns here; they're derived at read time against whichever EmployeeScheduleAssignment was
/// effective on this record's Date (see GetDailyTimeRecordQueryHandler), never cached, so a later
/// schedule correction can't leave a stale computed value behind. See HrModuleContext.md §3.6.
/// </summary>
public sealed class DailyTimeRecord : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;
    public DateTimeOffset Date { get; set; }
    public DateTimeOffset? TimeIn { get; set; }
    public DateTimeOffset? TimeOut { get; set; }
    public DateTimeOffset? BreakOutTime { get; set; }
    public DateTimeOffset? BreakInTime { get; set; }

    // MANUAL | KIOSK | BIOMETRIC_IMPORT — all three first-class, decided 2026-09-13 (§3.6).
    public string Source { get; set; } = default!;
    public string? Remarks { get; set; }
}
