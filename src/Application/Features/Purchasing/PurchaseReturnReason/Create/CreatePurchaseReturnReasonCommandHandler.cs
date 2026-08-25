namespace ZARI.Application.Features.Purchasing.PurchaseReturnReasons.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseReturnReasons.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreatePurchaseReturnReasonCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<CreatePurchaseReturnReasonCommand, Result<PurchaseReturnReasonResponse>>
{
    public async Task<Result<PurchaseReturnReasonResponse>> HandleAsync(CreatePurchaseReturnReasonCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("PURCHASE_RETURN_REASONS", FormAction.Create, cancellationToken))
            return Result.Failure<PurchaseReturnReasonResponse>(Error.Forbidden("PurchaseReturnReason.Forbidden", "You do not have permission to create purchase return reasons."));

        var codeExists = await dbContext.PurchaseReturnReasons.AnyAsync(r => r.Code == command.Code, cancellationToken);
        if (codeExists)
            return Result.Failure<PurchaseReturnReasonResponse>(Error.Conflict("PurchaseReturnReason.DuplicateCode", $"A purchase return reason with code '{command.Code}' already exists."));

        var reason = new PurchaseReturnReason
        {
            Code = command.Code,
            Description = command.Description,
            Status = command.Status
        };

        dbContext.PurchaseReturnReasons.Add(reason);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new PurchaseReturnReasonResponse(reason.Id, reason.Code, reason.Description, reason.Status, reason.CreatedAt);
        return Result.Success(response);
    }
}
