namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class SalesReturnLine : BaseEntity
{
    public Guid SalesReturnId { get; set; }
    public SalesReturn SalesReturn { get; set; } = default!;
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;
    public decimal QtyReturned { get; set; }
    public Guid UomId { get; set; }
    public Uom Uom { get; set; } = default!;

    // Copied from the original sale (the delivery line's UnitCost for the stock/COGS side is
    // resolved independently at Approve time) — this is the price the AR/revenue-side credit
    // memo is computed at.
    public decimal UnitPrice { get; set; }

    // Set only when the return itself references a DeliveryOrder — which of that delivery's lines
    // this line is returning against. Caps how much can be returned against that delivery line
    // (Phase 18 pattern) so the same shipped qty can't be over-returned across multiple returns.
    public Guid? DeliveryOrderLineId { get; set; }
    public DeliveryOrderLine? DeliveryOrderLine { get; set; }

    // Which physical unit is coming back, for a serialized item — optional/best-effort: only ever
    // meaningful for a return of a POS-originated sale (the only place a serial was ever recorded
    // as sold in the first place). SalesReturnPostingService reverses it (SOLD -> IN_STOCK) via
    // ReverseIssueSerialCommand when present; absent, the return posts exactly as it did before
    // this field existed (no serial-status effect).
    public string? SerialNo { get; set; }
}
