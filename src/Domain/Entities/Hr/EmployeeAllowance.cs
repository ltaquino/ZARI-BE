namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// A standing recurring allowance (e.g. monthly transportation allowance) — picked up
/// automatically by every Payroll Run whose period falls inside [EffectiveDate, EndDate]. Distinct
/// from PayrollLineAdjustment (ad-hoc, one-off, tied to a single run — not built until Payroll
/// itself is). See HrModuleContext.md §3.11.
/// </summary>
public sealed class EmployeeAllowance : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public decimal Amount { get; set; }
    public DateTimeOffset EffectiveDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public string Status { get; set; } = default!;
}
