namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// A customer commitment — no stock or GL effect on Approve, exactly like PurchaseOrder mirrors it
/// on the buy side. Delivery references this to fulfill it.
/// </summary>
public sealed class SalesOrder : AuditableEntity
{
    public string SoNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;
    public DateTimeOffset OrderDate { get; set; }
    public DateTimeOffset? ExpectedDeliveryDate { get; set; }
    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }

    // Header discount — manual only, percent-only for v1, applied after line-level discounts.
    // Subject to the same Company.MaxUnapprovedDiscountPct threshold as line discounts.
    public decimal? DiscountPct { get; set; }

    public List<SalesOrderLine> Lines { get; set; } = [];

    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
