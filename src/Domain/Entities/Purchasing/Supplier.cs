namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class Supplier : AuditableEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? TaxId { get; set; }
    public string? PaymentTerms { get; set; }

    public string? CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    public Guid? ApAccountId { get; set; }
    public GlAccount? ApAccount { get; set; }

    public string? Address { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactNumber { get; set; }
    public string? Email { get; set; }
    public string Status { get; set; } = default!;
}
