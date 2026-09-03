namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class SalesInvoiceLine : BaseEntity
{
    public Guid SalesInvoiceId { get; set; }
    public SalesInvoice SalesInvoice { get; set; } = default!;
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;
    public decimal Qty { get; set; }
    public Guid UomId { get; set; }
    public Uom Uom { get; set; } = default!;
    public decimal UnitPrice { get; set; }
    public decimal DiscountPct { get; set; }
    public string? DiscountSourceType { get; set; }
    public Guid? DiscountSourceId { get; set; }

    // "VATABLE" / "VAT_EXEMPT" / "ZERO_RATED" — defaults from Item.VatType, overridable. Forced to
    // VAT_EXEMPT whenever StatutoryDiscountTypeId is set (see SalesInvoiceLineCalculator).
    public string VatType { get; set; } = "VATABLE";

    // Statutory/special-law discount (Senior Citizen, PWD, National Athlete, Solo Parent — see
    // StatutoryDiscountType). When set, overrides DiscountPct/DiscountSourceType above entirely —
    // a line never combines a discretionary discount with a statutory one.
    public Guid? StatutoryDiscountTypeId { get; set; }
    public StatutoryDiscountType? StatutoryDiscountType { get; set; }
    public string? StatutoryIdNumber { get; set; }

    // Set only when the invoice itself references a DeliveryOrder — which of that delivery's lines
    // this line is billing. Caps how much can be invoiced against that delivery line (Phase 18
    // pattern) so the same shipped qty can't be over-invoiced across multiple invoices.
    public Guid? DeliveryOrderLineId { get; set; }
    public DeliveryOrderLine? DeliveryOrderLine { get; set; }

    // Which physical unit this line sold, for a serialized item — only ever populated on a
    // POS-originated line (DeliveryOrderLineId null): POS is the only Sales flow that actually
    // moves stock at invoice time (PosStockPostingService), so it's the only one with a real
    // moment to capture this. A Delivery-linked line has no equivalent field on DeliveryOrderLine
    // either — Delivery Order doesn't track serials at all, a known, separate, wider gap.
    public string? SerialNo { get; set; }
}
