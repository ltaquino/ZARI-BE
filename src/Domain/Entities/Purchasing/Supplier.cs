namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class Supplier : AuditableEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? TaxId { get; set; }
    /// Net payment days (0 = due on receipt/COD, 30/60/etc. = net days). Null means no default terms
    /// configured — AP Invoice's due date stays purely manual for that supplier.
    public int? PaymentTermsDays { get; set; }

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
