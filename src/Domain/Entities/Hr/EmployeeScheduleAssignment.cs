namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// A history table, not a direct Employee.WorkScheduleId FK — a shift reassignment must never
/// retroactively reinterpret already-recorded DTR entries (same reasoning as LoanAccount
/// snapshotting LoanProduct's rate at disbursement). EndDate null means "current/open-ended".
/// See HrModuleContext.md §3.5.
/// </summary>
public sealed class EmployeeScheduleAssignment : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;
    public Guid WorkScheduleId { get; set; }
    public WorkSchedule WorkSchedule { get; set; } = default!;
    public DateTimeOffset EffectiveDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
}
