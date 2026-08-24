namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class StockAdjustment : AuditableEntity
{
    public string AdjustmentNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;
    public DateTimeOffset AdjustmentDate { get; set; }

    // AdjustmentReason code, used to resolve the GL variance account — optional, falls back to
    // the default Inventory Variance account (see ApproveStockAdjustmentCommandHandler).
    public string? ReasonCode { get; set; }

    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }
    public List<StockAdjustmentLine> Lines { get; set; } = [];

    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
