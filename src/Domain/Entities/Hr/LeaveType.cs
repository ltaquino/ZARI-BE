namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Global master data. Exact entitlements (AccrualAmount, MaxCarryOverDays) are NOT invented here —
/// DOLE-regulated + this cooperative's own CBA/personnel manual specifics, coordinate with HR
/// before seeding real values. See ZARI-FE/frs/human-resource/HrModuleContext.md §3.7.
/// </summary>
public sealed class LeaveType : AuditableEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;

    // "MONTHLY" | "ANNUAL" — plain string, this codebase's convention.
    public string AccrualFrequency { get; set; } = default!;
    public decimal AccrualAmount { get; set; }
    public decimal? MaxCarryOverDays { get; set; }
    public bool IsPaid { get; set; }
    public bool RequiresDocumentation { get; set; }
    public string Status { get; set; } = default!;
}
