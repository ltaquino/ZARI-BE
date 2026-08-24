namespace ZARI.Application.Features.Uoms.Delete;

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

        dbContext.Uoms.Remove(uom);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
