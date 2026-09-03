namespace ZARI.Application.Features.Sales.PosPromoSlides.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeletePosPromoSlideCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeletePosPromoSlideCommand>
{
    public async Task<Result> HandleAsync(DeletePosPromoSlideCommand command, CancellationToken cancellationToken = default)
    {
        var slide = await dbContext.PosPromoSlides.FindAsync([command.Id], cancellationToken);
        if (slide is null)
            return Result.Failure(Error.NotFound("PosPromoSlide.NotFound", $"Promo slide with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("POS_PROMO_SLIDES", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("PosPromoSlide.Forbidden", "You do not have permission to delete promo slides."));

        dbContext.PosPromoSlides.Remove(slide);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
