namespace ZARI.Application.Features.Sales.PosPromoSlides.Update;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdatePosPromoSlideCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<UpdatePosPromoSlideCommand>
{
    public async Task<Result> HandleAsync(UpdatePosPromoSlideCommand command, CancellationToken cancellationToken = default)
    {
        var slide = await dbContext.PosPromoSlides.FindAsync([command.Id], cancellationToken);
        if (slide is null)
            return Result.Failure(Error.NotFound("PosPromoSlide.NotFound", $"Promo slide with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("POS_PROMO_SLIDES", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("PosPromoSlide.Forbidden", "You do not have permission to update promo slides."));

        slide.Title = command.Title;
        slide.Subtitle = command.Subtitle;
        slide.ImageUrl = command.ImageUrl;
        slide.DisplayOrder = command.DisplayOrder;
        slide.Status = command.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
