namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class StockAdjustment : AuditableEntity
{
    public string AdjustmentNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;
    public DateTimeOffset AdjustmentDate { get; set; }

    // AdjustmentReason code, used to resolve the GL variance account — optional, falls back to
    // the default Inventory Variance account (see ApproveStockAdjustmentCommandHandler).
    public string? ReasonCode { get; set; }

    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }
    public List<StockAdjustmentLine> Lines { get; set; } = [];

    // Optional departmental tag, applied to every line of this document's posted GL journal.
    public Guid? CostCenterId { get; set; }
    public CostCenter? CostCenter { get; set; }

    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
