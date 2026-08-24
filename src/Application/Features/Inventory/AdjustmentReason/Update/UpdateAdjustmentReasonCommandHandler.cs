namespace ZARI.Application.Features.Inventory.AdjustmentReasons.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateAdjustmentReasonCommandHandler(IAppDbContext dbContext) : ICommandHandler<UpdateAdjustmentReasonCommand>
{
    public async Task<Result> HandleAsync(UpdateAdjustmentReasonCommand command, CancellationToken cancellationToken = default)
    {
        var reason = await dbContext.AdjustmentReasons.FindAsync([command.Id], cancellationToken);
        if (reason is null)
            return Result.Failure(Error.NotFound("AdjustmentReason.NotFound", $"Adjustment reason with ID '{command.Id}' was not found."));

        var duplicateCode = await dbContext.AdjustmentReasons
            .AnyAsync(r => r.Id != command.Id && r.Code == command.Code, cancellationToken);

        if (duplicateCode)
            return Result.Failure(Error.Conflict("AdjustmentReason.DuplicateCode", $"An adjustment reason with code '{command.Code}' already exists."));

        reason.Code = command.Code;
        reason.Description = command.Description;
        reason.GlAccountId = command.GlAccountId;
        reason.Status = command.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
