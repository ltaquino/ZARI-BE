namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class PurchaseReturnReason : AuditableEntity
{
    public string Code { get; set; } = default!;
    public string? Description { get; set; }
    public string Status { get; set; } = default!;
}
