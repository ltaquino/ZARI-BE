namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Global master data — Pag-IBIG's real table is a fixed-peso amount per salary bracket (not a
/// percentage), same shape as SssContributionBracket (confirmed with the cooperative: SSS and
/// PhilHealth are salary-range matrices, but Pag-IBIG is a fixed amount — originally this session
/// unified Pag-IBIG with PhilHealth's percentage-based GovernmentContributionRate, which was wrong;
/// split out here once corrected). A single wide bracket (e.g. 0 to a very large SalaryTo) models a
/// truly flat amount for every employee; multiple rows model real salary-dependent tiers if ever
/// needed. Exact bracket values are NOT invented here — a real, externally-published,
/// periodically-updated government table; source from the actual current Pag-IBIG issuance at
/// build/seed time. See HrModuleContext.md §3.12.
/// </summary>
public sealed class PagIbigContributionBracket : AuditableEntity
{
    public decimal SalaryFrom { get; set; }
    public decimal SalaryTo { get; set; }
    public decimal EmployeeShare { get; set; }
    public decimal EmployerShare { get; set; }
    public DateTimeOffset EffectiveDate { get; set; }
    public string Status { get; set; } = default!;
}
