namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// A standing recurring deduction (e.g. a fixed-term loan-amortization deduction) — same shape as
/// EmployeeAllowance, opposite sign in payroll computation. Government-mandated deductions
/// (SSS/PhilHealth/Pag-IBIG/withholding tax) are computed from the contribution/bracket tables,
/// never stored here. See HrModuleContext.md §3.11.
/// </summary>
public sealed class EmployeeDeduction : AuditableEntity
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
