namespace ZARI.Application.Features.Inventory.StorageLocations.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteStorageLocationCommandHandler(IAppDbContext dbContext) : ICommandHandler<DeleteStorageLocationCommand>
{
    public async Task<Result> HandleAsync(DeleteStorageLocationCommand command, CancellationToken cancellationToken = default)
    {
        var location = await dbContext.StorageLocations.FindAsync([command.Id], cancellationToken);
        if (location is null)
            return Result.Failure(Error.NotFound("StorageLocation.NotFound", $"Storage location with ID '{command.Id}' was not found."));

        dbContext.StorageLocations.Remove(location);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
