namespace ZARI.Application.Features.Sales.PosPromoSlides.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetPosPromoSlideQuery(Guid Id) : IQuery<Result<PosPromoSlideResponse>>;

public sealed record PosPromoSlideResponse(
    Guid Id,
    string Title,
    string? Subtitle,
    string? ImageUrl,
    int DisplayOrder,
    string Status,
    DateTimeOffset CreatedAt);
