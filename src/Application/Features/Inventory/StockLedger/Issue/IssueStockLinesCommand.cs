namespace ZARI.Application.Features.Inventory.StockLedgers.Issue;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record IssueStockLineItem(
    Guid ItemId,
    string BranchId,
    Guid WarehouseId,
    string? BatchNo,
    decimal Qty,
    string ReferenceTable,
    string ReferenceId,
    DateTimeOffset TransactionDate,
    string? TransactionType);

public sealed record IssueStockLinesCommand(List<IssueStockLineItem> Lines) : ICommand<Result<IssueStockLinesResponse>>;

/// Keyed by ReferenceId (the caller's line id) — the costing-engine-computed unit cost for that
/// line, so the caller can stamp it onto its own transaction line. Only present for lines whose
/// item is stocked; a non-stocked item's line is silently skipped, matching the FE engine.
public sealed record IssueStockLinesResponse(Dictionary<string, decimal> CostsByReferenceId);
