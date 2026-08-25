namespace ZARI.Application.Features.Inventory.StorageLocations.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteStorageLocationCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteStorageLocationCommand>
{
    public async Task<Result> HandleAsync(DeleteStorageLocationCommand command, CancellationToken cancellationToken = default)
    {
        var location = await dbContext.StorageLocations.FindAsync([command.Id], cancellationToken);
        if (location is null)
            return Result.Failure(Error.NotFound("StorageLocation.NotFound", $"Storage location with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("STORAGE_LOCATIONS", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("StorageLocation.Forbidden", "You do not have permission to delete storage locations."));

        dbContext.StorageLocations.Remove(location);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
