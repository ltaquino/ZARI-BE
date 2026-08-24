namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class StockTransferRequest : AuditableEntity
{
    public string RequestNo { get; set; } = default!;

    // The branch being asked to fulfill/ship — the eventual Goods Issue's source branch.
    public string SourceBranchId { get; set; } = default!;
    public Branch SourceBranch { get; set; } = default!;
    public Guid SourceWarehouseId { get; set; }
    public Warehouse SourceWarehouse { get; set; } = default!;

    // The requesting branch that will receive — the eventual Goods Issue's destination branch.
    public string DestBranchId { get; set; } = default!;
    public Branch DestBranch { get; set; } = default!;
    public Guid DestWarehouseId { get; set; }
    public Warehouse DestWarehouse { get; set; } = default!;

    public DateTimeOffset RequestDate { get; set; }
    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }
    public List<StockTransferRequestLine> Lines { get; set; } = [];

    // Set when the fulfilling (source) branch declines an already-APPROVED request.
    public string? DeclinedBy { get; set; }
    public DateTimeOffset? DeclinedAt { get; set; }
    public string? DeclineReason { get; set; }

    // Set when the requesting (dest) branch withdraws its own request.
    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
