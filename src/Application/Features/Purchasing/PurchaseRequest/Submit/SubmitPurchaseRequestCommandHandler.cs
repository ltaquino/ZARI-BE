namespace ZARI.Application.Features.Purchasing.PurchaseRequests.Submit;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Purchasing.PurchaseRequests.GetAll;
using ZARI.Application.Features.Purchasing.PurchaseRequests.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>DRAFT -> PENDING_APPROVAL. Creates the ApprovalRequest a checker will act on.</summary>
public sealed class SubmitPurchaseRequestCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<SubmitPurchaseRequestCommand, Result<PurchaseRequestResponse>>
{
    public async Task<Result<PurchaseRequestResponse>> HandleAsync(SubmitPurchaseRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.PurchaseRequests
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (request is null)
            return Result.Failure<PurchaseRequestResponse>(Error.NotFound("PurchaseRequest.NotFound", $"Purchase request with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("PURCHASE_REQUESTS", FormAction.Edit, request.BranchId, cancellationToken))
            return Result.Failure<PurchaseRequestResponse>(Error.Forbidden("PurchaseRequest.Forbidden", "You do not have permission to submit this purchase request for this branch."));

        if (request.Status != "DRAFT")
            return Result.Failure<PurchaseRequestResponse>(Error.Validation("PurchaseRequest.NotDraft", "Only draft purchase requests can be submitted for approval."));

        if (request.Lines.Count == 0)
            return Result.Failure<PurchaseRequestResponse>(Error.Validation("PurchaseRequest.NoLines", "Add at least one line before submitting for approval."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("PURCHASE_REQUEST", request.Id.ToString(), request.BranchId, command.RequestedBy, null, null),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<PurchaseRequestResponse>(submitResult.Error!);

        request.Status = "PENDING_APPROVAL";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("PURCHASE_REQUEST", request.Id.ToString(), request.BranchId, "SUBMITTED", "APPROVAL_NEEDED",
                "submitted this purchase request for approval", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<PurchaseRequestResponse>(notifyResult.Error!);

        return Result.Success(PurchaseRequestMapper.ToResponse(request));
    }
}
