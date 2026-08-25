namespace ZARI.Domain.Entities;

// Gets a string Id (not the usual Guid) for the same reason as Branch: Company.BaseCurrencyId
// already stores the plain string "cur-php" in the live seeded DB, so matching that exactly lets
// the FK become real with no data migration.
public sealed class Currency
{
    public string Id { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? Name { get; set; }
    public string Status { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
