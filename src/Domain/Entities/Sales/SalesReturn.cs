namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Mirror-image reversal of a Delivery: receives stock back in and reverses both the COGS/
/// Inventory posting and the revenue-side posting (credit-memo shape — Dr Sales Returns &amp;
/// Allowances + Dr VAT Payable / Cr AR) at the original sale's own price.
/// </summary>
public sealed class SalesReturn : AuditableEntity
{
    public string ReturnNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    // Optional — a return doesn't have to trace back to a specific Delivery on file.
    public Guid? DeliveryOrderId { get; set; }
    public DeliveryOrder? DeliveryOrder { get; set; }

    public DateTimeOffset ReturnDate { get; set; }
    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }
    public List<SalesReturnLine> Lines { get; set; } = [];

    public Guid? CostCenterId { get; set; }
    public CostCenter? CostCenter { get; set; }

    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
