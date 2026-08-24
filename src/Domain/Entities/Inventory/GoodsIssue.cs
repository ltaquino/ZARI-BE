namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class GoodsIssue : AuditableEntity
{
    public string GiNo { get; set; } = default!;
    public string BranchId { get; set; } = default!; // source branch
    public Guid WarehouseId { get; set; } // source warehouse
    public Warehouse Warehouse { get; set; } = default!;
    public string ReferenceType { get; set; } = default!;

    // Only set (and only meaningful) when ReferenceType == STOCK_TRANSFER.
    public string? DestBranchId { get; set; }
    public Guid? DestWarehouseId { get; set; }
    public Warehouse? DestWarehouse { get; set; }

    // Required for INTERNAL_USE/DISPOSAL/PRODUCTION — which AdjustmentReason's GL account this posts its variance to.
    public string? ReasonCode { get; set; }

    public DateTimeOffset GiDate { get; set; }
    public string Status { get; set; } = default!;

    // Physical shipment tracking for the interbranch transfer leg, independent of Status — only
    // meaningful once Status is POSTED. Null for non-transfer reference types.
    public string? ShipmentStatus { get; set; }

    public string? Remarks { get; set; }
    public List<GoodsIssueLine> Lines { get; set; } = [];

    // StockTransferRequest isn't a backend entity yet, so this stays a plain string (not Guid/FK)
    // until it exists — same pattern as GoodsReceipt.GoodsIssueId.
    public string? StockTransferRequestRefNo { get; set; }
    public string? StockTransferRequestId { get; set; }

    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
