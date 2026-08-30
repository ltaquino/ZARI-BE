namespace ZARI.Application.Features.Sales.StatutoryDiscountTypes.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteStatutoryDiscountTypeCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteStatutoryDiscountTypeCommand>
{
    public async Task<Result> HandleAsync(DeleteStatutoryDiscountTypeCommand command, CancellationToken cancellationToken = default)
    {
        var type = await dbContext.StatutoryDiscountTypes.FindAsync([command.Id], cancellationToken);
        if (type is null)
            return Result.Failure(Error.NotFound("StatutoryDiscountType.NotFound", $"Statutory discount type with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("STATUTORY_DISCOUNT_TYPES", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("StatutoryDiscountType.Forbidden", "You do not have permission to delete statutory discount types."));

        dbContext.StatutoryDiscountTypes.Remove(type);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
