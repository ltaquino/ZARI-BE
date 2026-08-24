namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

// A bin-to-bin move within one warehouse — distinct from an interbranch transfer (Goods
// Issue+Receipt). Deliberately does not touch StockLedger: qty leaving one bin exactly equals
// qty entering another in the same warehouse, netting to zero at the ledger's (item, branch,
// warehouse, batch) grain. No GL journal either — no branch, ownership, or valuation change means
// nothing financial happened. Simple single-step post, no approval workflow — low-risk,
// operational only. Cancel is DRAFT-only (nothing posted yet to reverse), so unlike the other
// inventory documents there is no PENDING_CANCELLATION tier at all.
public sealed class StockLocationTransfer : AuditableEntity
{
    public string TransferNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;
    public DateTimeOffset TransferDate { get; set; }
    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }
    public List<StockLocationTransferLine> Lines { get; set; } = [];

    public string? PostedBy { get; set; }

    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
