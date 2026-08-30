namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

// A single-row settings entity — there is exactly one Company record, seeded once at startup
// (see AppDbSeeder.SeedCompanyAsync) and only ever updated, never created or deleted through the
// API. Mirrors the FE mock's company.ts, which stores one object rather than a list.
public sealed class Company : AuditableEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? TaxId { get; set; }

    public string BaseCurrencyId { get; set; } = default!;
    public Currency BaseCurrency { get; set; } = default!;

    // BIR (Bureau of Internal Revenue, Philippines) compliance fields — company-wide, since VAT
    // registration and the registered address are properties of the legal entity (the TIN itself),
    // not any one branch. Per-branch/per-POS-machine BIR details (permit numbers, MIN) live on
    // Branch instead. All optional/nullable — additive fields, no backfill required.
    public string? RegisteredAddress { get; set; }
    public string? TradeName { get; set; }
    /// "VAT" or "NON_VAT" — determines whether receipts need a VATable/VAT-Exempt/Zero-Rated
    /// breakdown or the simpler non-VAT boilerplate.
    public string? VatRegistrationType { get; set; }
}
