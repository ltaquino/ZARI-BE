namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Global master data, unifying PhilHealth and Pag-IBIG (both real-world percentage-of-salary
/// schemes with a salary bracket and an employee/employer split, unlike SSS's fixed-peso-per-
/// bracket table, which stays its own entity — see SssContributionBracket). Effective-dated, same
/// "policy lives in data" reasoning. Exact rate/cap values are NOT invented here — real,
/// externally-published PhilHealth/Pag-IBIG issuances; source at build/seed time. See
/// HrModuleContext.md §3.12.
/// </summary>
public sealed class GovernmentContributionRate : AuditableEntity
{
    // "PHILHEALTH" | "PAGIBIG"
    public string ContributionType { get; set; } = default!;
    public decimal SalaryFrom { get; set; }
    public decimal SalaryTo { get; set; }
    public decimal EmployeeSharePct { get; set; }
    public decimal EmployerSharePct { get; set; }
    public decimal? MaxContribution { get; set; }
    public DateTimeOffset EffectiveDate { get; set; }
    public string Status { get; set; } = default!;
}
