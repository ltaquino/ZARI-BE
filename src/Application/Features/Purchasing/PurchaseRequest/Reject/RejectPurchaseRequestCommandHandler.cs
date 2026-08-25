namespace ZARI.Application.Features.Purchasing.PurchaseRequests.Reject;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Purchasing.PurchaseRequests.GetAll;
using ZARI.Application.Features.Purchasing.PurchaseRequests.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>PENDING_APPROVAL -> DRAFT, so the encoder can fix the issue the checker flagged and resubmit.</summary>
public sealed class RejectPurchaseRequestCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RejectPurchaseRequestCommand, Result<PurchaseRequestResponse>>
{
    public async Task<Result<PurchaseRequestResponse>> HandleAsync(RejectPurchaseRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.PurchaseRequests
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (request is null)
            return Result.Failure<PurchaseRequestResponse>(Error.NotFound("PurchaseRequest.NotFound", $"Purchase request with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("PURCHASE_REQUESTS", FormAction.Approve, request.BranchId, cancellationToken))
            return Result.Failure<PurchaseRequestResponse>(Error.Forbidden("PurchaseRequest.Forbidden", "You do not have permission to reject this purchase request for this branch."));

        if (request.Status != "PENDING_APPROVAL")
            return Result.Failure<PurchaseRequestResponse>(Error.Validation("PurchaseRequest.NotPendingApproval", "Only purchase requests pending approval can be rejected."));

        var approvalRequest = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "PURCHASE_REQUEST" && r.EntityId == request.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (approvalRequest is null)
            return Result.Failure<PurchaseRequestResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this purchase request."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(approvalRequest.Id, command.ApproverUserId, "Reject", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<PurchaseRequestResponse>(decideResult.Error!);

        request.Status = "DRAFT";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("PURCHASE_REQUEST", request.Id.ToString(), request.BranchId, "REJECTED", "ACTIVITY",
                $"rejected this purchase request — \"{command.Comments}\"", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<PurchaseRequestResponse>(notifyResult.Error!);

        return Result.Success(PurchaseRequestMapper.ToResponse(request));
    }
}
