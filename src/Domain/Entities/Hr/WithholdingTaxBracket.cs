namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Global master data — the standard BIR withholding-tax-table shape (a fixed base tax per
/// bracket plus a percentage of the excess over the bracket floor), effective-dated. Exact bracket
/// values are NOT invented here — the real, current BIR withholding tax table; source at
/// build/seed time. See HrModuleContext.md §3.12.
/// </summary>
public sealed class WithholdingTaxBracket : AuditableEntity
{
    public decimal SalaryFrom { get; set; }
    public decimal SalaryTo { get; set; }
    public decimal BaseTax { get; set; }
    public decimal RateOverExcess { get; set; }
    public DateTimeOffset EffectiveDate { get; set; }
    public string Status { get; set; } = default!;
}
