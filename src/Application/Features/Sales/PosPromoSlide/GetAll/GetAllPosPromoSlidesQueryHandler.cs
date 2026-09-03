namespace ZARI.Application.Features.Sales.PosPromoSlides.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.PosPromoSlides.Get;
using ZARI.Domain.Common;

public sealed class GetAllPosPromoSlidesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllPosPromoSlidesQuery, Result<List<PosPromoSlideResponse>>>
{
    public async Task<Result<List<PosPromoSlideResponse>>> HandleAsync(GetAllPosPromoSlidesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("POS_PROMO_SLIDES", FormAction.View, cancellationToken))
            return Result.Failure<List<PosPromoSlideResponse>>(Error.Forbidden("PosPromoSlide.Forbidden", "You do not have permission to view promo slides."));

        var items = await dbContext.PosPromoSlides.AsNoTracking()
            .OrderBy(s => s.DisplayOrder).ThenBy(s => s.Title)
            .Select(s => new PosPromoSlideResponse(s.Id, s.Title, s.Subtitle, s.ImageUrl, s.DisplayOrder, s.Status, s.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
