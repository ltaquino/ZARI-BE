namespace ZARI.Application.Features.Sales.PosPromoSlides.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.PosPromoSlides.Get;
using ZARI.Domain.Common;

public sealed record GetAllPosPromoSlidesQuery : IQuery<Result<List<PosPromoSlideResponse>>>;
