namespace ZARI.Application.Features.Inventory.StockLedgers.Shared;

/// One consumed FIFO cost layer, recorded on a Fifo issue so a later reversal can restore it exactly.
public sealed record ConsumptionDto(Guid LayerId, decimal Qty);

/// One batch balance bucket drawn from on an Avg-costed issue, recorded so a later reversal can
/// restore each bucket at the cost it actually held at draw time.
public sealed record BalanceDrawDto(string? BatchNo, decimal Qty, decimal UnitCost);
