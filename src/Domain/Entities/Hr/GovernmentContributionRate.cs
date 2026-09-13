namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Global master data — PhilHealth's real table is a percentage-of-salary bracket with an
/// employee/employer split. Originally this session also unified Pag-IBIG in here on the
/// assumption both were percentage-based; corrected once the cooperative confirmed Pag-IBIG is
/// actually a fixed-peso amount, same shape as SSS's fixed-peso-per-bracket table — see the
/// dedicated PagIbigContributionBracket entity instead. ContributionType is kept (rather than
/// removed) for forward extensibility should a future percentage-based government scheme need
/// this same shape, but only "PHILHEALTH" validates today. Effective-dated, same "policy lives in
/// data" reasoning. Exact rate/cap values are NOT invented here — real, externally-published
/// PhilHealth issuances; source at build/seed time. See HrModuleContext.md §3.12.
/// </summary>
public sealed class GovernmentContributionRate : AuditableEntity
{
    // "PHILHEALTH" only, as of the fix described above.
    public string ContributionType { get; set; } = default!;
    public decimal SalaryFrom { get; set; }
    public decimal SalaryTo { get; set; }
    public decimal EmployeeSharePct { get; set; }
    public decimal EmployerSharePct { get; set; }
    public decimal? MaxContribution { get; set; }
    public DateTimeOffset EffectiveDate { get; set; }
    public string Status { get; set; } = default!;
}
