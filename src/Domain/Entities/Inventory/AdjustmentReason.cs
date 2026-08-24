namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class AdjustmentReason : AuditableEntity
{
    public string Code { get; set; } = default!;
    public string? Description { get; set; }

    // GL account reference — the Accounting module isn't a backend entity yet,
    // so this stays a plain string (not Guid/FK) until it exists.
    public string? GlAccountId { get; set; }
    public string Status { get; set; } = default!;
}
