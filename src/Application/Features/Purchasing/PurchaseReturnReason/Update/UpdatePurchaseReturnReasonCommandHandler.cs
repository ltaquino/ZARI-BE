namespace ZARI.Application.Features.Purchasing.PurchaseReturnReasons.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdatePurchaseReturnReasonCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<UpdatePurchaseReturnReasonCommand>
{
    public async Task<Result> HandleAsync(UpdatePurchaseReturnReasonCommand command, CancellationToken cancellationToken = default)
    {
        var reason = await dbContext.PurchaseReturnReasons.FindAsync([command.Id], cancellationToken);
        if (reason is null)
            return Result.Failure(Error.NotFound("PurchaseReturnReason.NotFound", $"Purchase return reason with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("PURCHASE_RETURN_REASONS", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("PurchaseReturnReason.Forbidden", "You do not have permission to update purchase return reasons."));

        var duplicateCode = await dbContext.PurchaseReturnReasons
            .AnyAsync(r => r.Id != command.Id && r.Code == command.Code, cancellationToken);

        if (duplicateCode)
            return Result.Failure(Error.Conflict("PurchaseReturnReason.DuplicateCode", $"A purchase return reason with code '{command.Code}' already exists."));

        reason.Code = command.Code;
        reason.Description = command.Description;
        reason.Status = command.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
