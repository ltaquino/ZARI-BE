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

    // References the not-yet-migrated Currency module — plain string, matches Warehouse.BranchId.
    public string BaseCurrencyId { get; set; } = default!;
}
