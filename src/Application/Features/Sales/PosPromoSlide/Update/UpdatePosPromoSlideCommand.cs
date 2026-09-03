namespace ZARI.Application.Features.Sales.PosPromoSlides.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdatePosPromoSlideCommand(
    Guid Id,
    string Title,
    string? Subtitle,
    string? ImageUrl,
    int DisplayOrder,
    string Status) : ICommand;
