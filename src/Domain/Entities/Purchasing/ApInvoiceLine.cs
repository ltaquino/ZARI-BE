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
}
