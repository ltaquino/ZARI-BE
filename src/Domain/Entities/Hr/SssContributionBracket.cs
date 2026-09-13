namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Global master data — SSS's real table is a fixed-peso amount per salary bracket (not a
/// percentage), effective-dated so a future SSS circular's rate change is a data edit, not a
/// redeploy. Exact bracket values are NOT invented here — a real, externally-published,
/// periodically-updated government table; source from the actual current SSS issuance at
/// build/seed time. See HrModuleContext.md §3.12.
/// </summary>
public sealed class SssContributionBracket : AuditableEntity
{
    public decimal SalaryFrom { get; set; }
    public decimal SalaryTo { get; set; }
    public decimal EmployeeShare { get; set; }
    public decimal EmployerShare { get; set; }
    public DateTimeOffset EffectiveDate { get; set; }
    public string Status { get; set; } = default!;
}
