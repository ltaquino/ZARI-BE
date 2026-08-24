namespace ZARI.Application.Features.Inventory.StockLedgers.Reverse;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>
/// Reverses every line of a previously posted document, identified by (ReferenceTable,
/// ReferenceId) per line — used when a POSTED document's cancellation is finally approved. A line
/// with no matching ledger row (e.g. a non-stocked item) is silently skipped.
/// </summary>
public sealed record ReverseStockMovementsCommand(string ReferenceTable, List<string> ReferenceIds) : ICommand<Result>;
