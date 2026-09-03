namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// One slide in the customer-facing display's idle-time promotional slideshow. ImageUrl points at
/// an uploaded file served from wwwroot/uploads/pos-promo-slides/ (see the upload endpoint on
/// PosPromoSlideEndpoints) — a slide with no image just shows its Title/Subtitle as text.
/// </summary>
public sealed class PosPromoSlide : AuditableEntity
{
    public string Title { get; set; } = default!;
    public string? Subtitle { get; set; }
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public string Status { get; set; } = default!;
}
