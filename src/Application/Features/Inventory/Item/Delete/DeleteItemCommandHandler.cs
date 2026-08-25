namespace ZARI.Application.Features.Inventory.Items.Delete;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteItemCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteItemCommand>
{
    public async Task<Result> HandleAsync(DeleteItemCommand command, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.Items.FindAsync([command.Id], cancellationToken);
        if (item is null)
            return Result.Failure(Error.NotFound("Item.NotFound", $"Item with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("ITEMS", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("Item.Forbidden", "You do not have permission to delete items."));

        var settingCount = await dbContext.ItemBranchSettings.CountAsync(s => s.ItemId == command.Id, cancellationToken);
        if (settingCount > 0)
            return Result.Failure(Error.Conflict("Item.HasReorderSettings", $"Cannot delete this item — it has {settingCount} reorder setting{(settingCount == 1 ? "" : "s")}."));

        var reservationCount = await dbContext.StockReservations.CountAsync(r => r.ItemId == command.Id, cancellationToken);
        if (reservationCount > 0)
            return Result.Failure(Error.Conflict("Item.HasReservations", $"Cannot delete this item — it has {reservationCount} stock reservation{(reservationCount == 1 ? "" : "s")}."));

        dbContext.Items.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
