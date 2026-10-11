namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// A barcode printed on an item's packaging (EAN-13, UPC-A, a supplier's own code…). An item can have several — e.g.
/// the single piece and the box of 12 — and each barcode belongs to exactly one item. Scanning one adds
/// <see cref="PackQty"/> of the item's base unit (ZARI has no unit conversions, so a box barcode is how a scan sells a
/// whole box).
/// </summary>
public sealed class ItemBarcode : AuditableEntity
{
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;
    public string Barcode { get; set; } = default!;
    public decimal PackQty { get; set; } = 1;
    /// <summary>Optional label for the pack, e.g. "Box of 12".</summary>
    public string? Description { get; set; }
}
