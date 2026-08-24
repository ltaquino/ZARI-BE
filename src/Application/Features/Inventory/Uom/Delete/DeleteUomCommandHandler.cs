namespace ZARI.Application.Features.Inventory.Uoms.Delete;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteUomCommandHandler(IAppDbContext dbContext) : ICommandHandler<DeleteUomCommand>
{
    public async Task<Result> HandleAsync(DeleteUomCommand command, CancellationToken cancellationToken = default)
    {
        var uom = await dbContext.Uoms.FindAsync([command.Id], cancellationToken);
        if (uom is null)
            return Result.Failure(Error.NotFound("Uom.NotFound", $"UOM with ID '{command.Id}' was not found."));

        var itemCount = await dbContext.Items.CountAsync(i => i.BaseUomId == command.Id, cancellationToken);
        if (itemCount > 0)
            return Result.Failure(Error.Conflict("Uom.InUse", $"Cannot delete this UOM — it is used by {itemCount} item{(itemCount == 1 ? "" : "s")}."));

        dbContext.Uoms.Remove(uom);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
