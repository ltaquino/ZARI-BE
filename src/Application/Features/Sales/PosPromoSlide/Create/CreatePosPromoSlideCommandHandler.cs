namespace ZARI.Application.Features.Sales.PosPromoSlides.Create;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.PosPromoSlides.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreatePosPromoSlideCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<CreatePosPromoSlideCommand, Result<PosPromoSlideResponse>>
{
    public async Task<Result<PosPromoSlideResponse>> HandleAsync(CreatePosPromoSlideCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("POS_PROMO_SLIDES", FormAction.Create, cancellationToken))
            return Result.Failure<PosPromoSlideResponse>(Error.Forbidden("PosPromoSlide.Forbidden", "You do not have permission to create promo slides."));

        var slide = new PosPromoSlide
        {
            Title = command.Title,
            Subtitle = command.Subtitle,
            ImageUrl = command.ImageUrl,
            DisplayOrder = command.DisplayOrder,
            Status = command.Status
        };

        dbContext.PosPromoSlides.Add(slide);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new PosPromoSlideResponse(slide.Id, slide.Title, slide.Subtitle, slide.ImageUrl, slide.DisplayOrder, slide.Status, slide.CreatedAt);
        return Result.Success(response);
    }
}
