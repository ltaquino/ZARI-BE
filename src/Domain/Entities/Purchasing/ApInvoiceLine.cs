namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class ApInvoiceLine : BaseEntity
{
    public Guid ApInvoiceId { get; set; }
    public ApInvoice ApInvoice { get; set; } = default!;
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;
    public decimal Qty { get; set; }
    public Guid UomId { get; set; }
    public Uom Uom { get; set; } = default!;
    public decimal UnitCost { get; set; }

    // Classification-only, for the Purchase Book (BIR books-of-accounts) report's VATable/
    // Zero-Rated/Exempt/Input-Tax breakdown — never read by GL posting (ApproveApInvoiceCommandHandler
    // posts UnitCost*Qty exactly as before this field existed). Defaults from the Item's own VatType
    // at Create time, same convention as SalesInvoiceLine.
    public string VatType { get; set; } = "VATABLE";

    // Set for every ITEM invoice line (ITEM invoices always bill against a GRPO) — which of that
    // receipt's lines this line is billing. Caps how much can be invoiced against that receipt line
    // (see CreateApInvoiceCommandHandler) so the same received qty can't be over-invoiced across
    // multiple AP invoices.
    public Guid? GoodsReceiptPoLineId { get; set; }
    public GoodsReceiptPoLine? GoodsReceiptPoLine { get; set; }
}
