namespace ZARI.Application.Features.Purchasing.PurchaseRequests.Cancel;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Purchasing.PurchaseRequests.GetAll;
using ZARI.Application.Features.Purchasing.PurchaseRequests.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// DRAFT / PENDING_APPROVAL / APPROVED -> CANCELLED. No posted-document two-tier flow needed —
/// this table has zero stock/GL impact, so a single-tier cancel is enough.
/// </summary>
public sealed class CancelPurchaseRequestCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CancelPendingApprovalRequestCommand, Result> cancelPendingHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CancelPurchaseRequestCommand, Result<PurchaseRequestResponse>>
{
    public async Task<Result<PurchaseRequestResponse>> HandleAsync(CancelPurchaseRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.PurchaseRequests
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (request is null)
            return Result.Failure<PurchaseRequestResponse>(Error.NotFound("PurchaseRequest.NotFound", $"Purchase request with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("PURCHASE_REQUESTS", FormAction.Cancel, request.BranchId, cancellationToken))
            return Result.Failure<PurchaseRequestResponse>(Error.Forbidden("PurchaseRequest.Forbidden", "You do not have permission to cancel this purchase request for this branch."));

        if (request.Status == "CANCELLED")
            return Result.Failure<PurchaseRequestResponse>(Error.Validation("PurchaseRequest.AlreadyCancelled", "This purchase request is already cancelled."));

        var cancelPendingResult = await cancelPendingHandler.HandleAsync(new CancelPendingApprovalRequestCommand("PURCHASE_REQUEST", request.Id.ToString()), cancellationToken);
        if (!cancelPendingResult.IsSuccess)
            return Result.Failure<PurchaseRequestResponse>(cancelPendingResult.Error!);

        request.Status = "CANCELLED";
        request.CancelledBy = command.CancelledBy;
        request.CancelledAt = DateTimeOffset.UtcNow;
        request.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("PURCHASE_REQUEST", request.Id.ToString(), request.BranchId, "CANCELLED", "ACTIVITY",
                $"cancelled this purchase request — \"{command.Reason}\"", command.CancelledBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<PurchaseRequestResponse>(notifyResult.Error!);

        return Result.Success(PurchaseRequestMapper.ToResponse(request));
    }
}
