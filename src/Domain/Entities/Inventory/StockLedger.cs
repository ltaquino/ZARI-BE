namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Immutable append-only movement log — source of truth for all stock movement. Rows are only ever
/// appended (by the Receive/Issue/Reverse stock-ledger handlers); nothing ever updates or deletes one.
/// </summary>
public sealed class StockLedger : AuditableEntity
{
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;

    // Item identity snapshot, same pattern as the transaction-line snapshot fields — this is the
    // ultimate immutable record, so a later item rename must never rewrite what it says here.
    public string? ItemCode { get; set; }
    public string? ItemName { get; set; }
    public string? UomCode { get; set; }

    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public string? BatchNo { get; set; }

    /// GOODS_RECEIPT | GOODS_ISSUE | STOCK_ADJUSTMENT | STOCK_OPNAME
    public string TransactionType { get; set; } = default!;

    public string ReferenceTable { get; set; } = default!;
    public string ReferenceId { get; set; } = default!;
    public decimal QtyIn { get; set; }
    public decimal QtyOut { get; set; }
    public decimal UnitCost { get; set; }
    public decimal RunningBalanceQty { get; set; }
    public decimal RunningBalanceValue { get; set; }

    /// Flags a row posted purely to undo a cancelled document.
    public bool IsReversal { get; set; }

    // JSON-serialized consumption/draw detail — only meaningful on a Fifo/Avg issue row, read back
    // solely by the reversal handler to restore the exact cost layers/balance buckets a line drew
    // from. Kept as raw JSON rather than a typed EF-mapped collection: it's never queried or
    // filtered on, only round-tripped whole, so a value-comparer/owned-collection mapping would be
    // pure overhead for no benefit.
    public string? ConsumptionsJson { get; set; }
    public string? BalanceDrawsJson { get; set; }

    public DateTimeOffset TransactionDate { get; set; }
    public DateTimeOffset PostedAt { get; set; }
}
