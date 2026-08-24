namespace ZARI.Application.Features.Inventory.AdjustmentReasons.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.AdjustmentReasons.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateAdjustmentReasonCommandHandler(IAppDbContext dbContext) : ICommandHandler<CreateAdjustmentReasonCommand, Result<AdjustmentReasonResponse>>
{
    public async Task<Result<AdjustmentReasonResponse>> HandleAsync(CreateAdjustmentReasonCommand command, CancellationToken cancellationToken = default)
    {
        var codeExists = await dbContext.AdjustmentReasons.AnyAsync(r => r.Code == command.Code, cancellationToken);
        if (codeExists)
            return Result.Failure<AdjustmentReasonResponse>(Error.Conflict("AdjustmentReason.DuplicateCode", $"An adjustment reason with code '{command.Code}' already exists."));

        var reason = new AdjustmentReason
        {
            Code = command.Code,
            Description = command.Description,
            GlAccountId = command.GlAccountId,
            Status = command.Status
        };

        dbContext.AdjustmentReasons.Add(reason);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new AdjustmentReasonResponse(reason.Id, reason.Code, reason.Description, reason.GlAccountId, reason.Status, reason.CreatedAt);
        return Result.Success(response);
    }
}
