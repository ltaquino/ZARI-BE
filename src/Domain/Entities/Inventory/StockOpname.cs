namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class StockOpname : AuditableEntity
{
    public string OpnameNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;
    public DateTimeOffset CountDate { get; set; }
    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }
    public List<StockOpnameLine> Lines { get; set; } = [];

    // Optional departmental tag, applied to every line of this document's posted GL journal.
    public Guid? CostCenterId { get; set; }
    public CostCenter? CostCenter { get; set; }

    // No RequestedBy/ApprovedBy — a branch manager posts the count directly, the physical count
    // itself is the evidence. Only the cancellation leg goes through the approval workflow.
    public string? PostedBy { get; set; }

    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
