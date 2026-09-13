namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Global master data — the OT pay multiplier per category, effective-dated (same "policy lives
/// in data, not code" idiom as LoanProduct.PenaltyRatePct), so a future DOLE-mandated rate change
/// is a data edit, not a redeploy. Exact multiplier VALUES are not invented here — Labor Code Art.
/// 87 + DOLE issuances, coordinate before seeding. See HrModuleContext.md §3.10.
/// </summary>
public sealed class OvertimeRateRule : AuditableEntity
{
    // "ORDINARY" | "REST_DAY" | "HOLIDAY"
    public string OtType { get; set; } = default!;
    public decimal Multiplier { get; set; }
    public DateTimeOffset EffectiveDate { get; set; }
    public string Status { get; set; } = default!;
}
