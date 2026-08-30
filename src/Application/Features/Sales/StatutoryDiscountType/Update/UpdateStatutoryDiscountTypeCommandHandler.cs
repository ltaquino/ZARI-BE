namespace ZARI.Application.Features.Sales.StatutoryDiscountTypes.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateStatutoryDiscountTypeCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<UpdateStatutoryDiscountTypeCommand>
{
    public async Task<Result> HandleAsync(UpdateStatutoryDiscountTypeCommand command, CancellationToken cancellationToken = default)
    {
        var type = await dbContext.StatutoryDiscountTypes.FindAsync([command.Id], cancellationToken);
        if (type is null)
            return Result.Failure(Error.NotFound("StatutoryDiscountType.NotFound", $"Statutory discount type with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("STATUTORY_DISCOUNT_TYPES", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("StatutoryDiscountType.Forbidden", "You do not have permission to update statutory discount types."));

        var duplicateCode = await dbContext.StatutoryDiscountTypes
            .AnyAsync(t => t.Id != command.Id && t.Code == command.Code, cancellationToken);

        if (duplicateCode)
            return Result.Failure(Error.Conflict("StatutoryDiscountType.DuplicateCode", $"A statutory discount type with code '{command.Code}' already exists."));

        type.Code = command.Code;
        type.Name = command.Name;
        type.DiscountPct = command.DiscountPct;
        type.IsVatExempt = command.IsVatExempt;
        type.RequiredIdLabel = command.RequiredIdLabel;
        type.Status = command.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
