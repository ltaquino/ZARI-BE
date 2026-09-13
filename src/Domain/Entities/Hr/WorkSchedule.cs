namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Global master data (like Department/Position) — a shift template shared across branches.
/// See ZARI-FE/frs/human-resource/HrModuleContext.md §3.4.
/// </summary>
public sealed class WorkSchedule : AuditableEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public TimeOnly TimeIn { get; set; }
    public TimeOnly TimeOut { get; set; }

    // Plain comma-list string (this codebase's "plain string over new enum type" convention),
    // e.g. "MON,TUE,WED,THU,FRI".
    public string WorkDays { get; set; } = default!;
    public int GracePeriodMinutes { get; set; }
    public int BreakMinutes { get; set; }
    public string Status { get; set; } = default!;
}
