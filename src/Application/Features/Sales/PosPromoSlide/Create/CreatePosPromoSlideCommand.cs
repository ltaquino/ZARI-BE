namespace ZARI.Application.Features.Sales.PosPromoSlides.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.PosPromoSlides.Get;
using ZARI.Domain.Common;

public sealed record CreatePosPromoSlideCommand(
    string Title,
    string? Subtitle,
    string? ImageUrl,
    int DisplayOrder,
    string Status) : ICommand<Result<PosPromoSlideResponse>>;
