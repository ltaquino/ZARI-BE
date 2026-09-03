namespace ZARI.Application.Features.Sales.PosPromoSlides.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetPosPromoSlideQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetPosPromoSlideQuery, Result<PosPromoSlideResponse>>
{
    public async Task<Result<PosPromoSlideResponse>> HandleAsync(GetPosPromoSlideQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("POS_PROMO_SLIDES", FormAction.View, cancellationToken))
            return Result.Failure<PosPromoSlideResponse>(Error.Forbidden("PosPromoSlide.Forbidden", "You do not have permission to view promo slides."));

        var slide = await dbContext.PosPromoSlides
            .Where(s => s.Id == query.Id)
            .Select(s => new PosPromoSlideResponse(s.Id, s.Title, s.Subtitle, s.ImageUrl, s.DisplayOrder, s.Status, s.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (slide is null)
            return Result.Failure<PosPromoSlideResponse>(Error.NotFound("PosPromoSlide.NotFound", $"Promo slide with ID '{query.Id}' was not found."));

        return Result.Success(slide);
    }
}
