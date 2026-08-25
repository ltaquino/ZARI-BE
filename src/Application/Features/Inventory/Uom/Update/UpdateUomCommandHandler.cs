namespace ZARI.Application.Features.Inventory.Uoms.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateUomCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<UpdateUomCommand>
{
    public async Task<Result> HandleAsync(UpdateUomCommand command, CancellationToken cancellationToken = default)
    {
        var uom = await dbContext.Uoms.FindAsync([command.Id], cancellationToken);
        if (uom is null)
            return Result.Failure(Error.NotFound("Uom.NotFound", $"UOM with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("UOMS", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("Uom.Forbidden", "You do not have permission to update UOMs."));

        var duplicateCode = await dbContext.Uoms
            .AnyAsync(u => u.Id != command.Id && u.Code == command.Code, cancellationToken);

        if (duplicateCode)
            return Result.Failure(Error.Conflict("Uom.DuplicateCode", $"A UOM with code '{command.Code}' already exists."));

        uom.Code = command.Code;
        uom.Name = command.Name;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
