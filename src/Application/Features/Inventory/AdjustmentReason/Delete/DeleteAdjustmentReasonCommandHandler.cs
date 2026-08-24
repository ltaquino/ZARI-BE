namespace ZARI.Application.Features.Inventory.AdjustmentReasons.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteAdjustmentReasonCommandHandler(IAppDbContext dbContext) : ICommandHandler<DeleteAdjustmentReasonCommand>
{
    public async Task<Result> HandleAsync(DeleteAdjustmentReasonCommand command, CancellationToken cancellationToken = default)
    {
        var reason = await dbContext.AdjustmentReasons.FindAsync([command.Id], cancellationToken);
        if (reason is null)
            return Result.Failure(Error.NotFound("AdjustmentReason.NotFound", $"Adjustment reason with ID '{command.Id}' was not found."));

        dbContext.AdjustmentReasons.Remove(reason);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
