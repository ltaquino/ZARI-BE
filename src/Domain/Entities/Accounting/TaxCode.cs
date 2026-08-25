namespace ZARI.Domain.Entities;

// Code is the primary key (not a generated Guid) — tax codes are a small, fixed reference list
// where the human-readable code ("VAT12") is the natural identity, matching the FE mock's exact
// id-equals-code behavior and Form's precedent for a string-keyed catalog entity in this codebase.
public sealed class TaxCode
{
    public string Code { get; set; } = default!;
    public string? Name { get; set; }
    public decimal Rate { get; set; }
    public string TaxType { get; set; } = default!;

    public Guid? GlAccountId { get; set; }
    public GlAccount? GlAccount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
