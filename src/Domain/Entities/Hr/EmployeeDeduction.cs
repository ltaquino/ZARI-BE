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
    /// <summary>A route-sales trip shortage charged to the driver at trip close (Dr 1220 Due from Employees).
    /// Created and owned by the trip close — HR can't create, edit or delete one by hand.</summary>
    public const string TripShortageCode = "TRIP-SHORT";

    /// <summary>
    /// Deduction codes that recover money the employee already owes the cooperative — booked to 1220 Due
    /// from Employees when it arose — so payroll credits 1220 for them instead of 2350 Other Payroll
    /// Deductions Payable. A List, not an array, so it translates inside EF queries under C# 14.
    /// </summary>
    public static readonly List<string> ReceivableRecoveryCodes = [TripShortageCode];

    public static bool IsSystemCode(string? code) => string.Equals(code?.Trim(), TripShortageCode, StringComparison.OrdinalIgnoreCase);

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public decimal Amount { get; set; }
    public DateTimeOffset EffectiveDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public string Status { get; set; } = default!;
}
