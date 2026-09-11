namespace ZARI.Application.Features.Loan.LoanRestructurings.Reject;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanRestructurings.GetAll;
using ZARI.Application.Features.Loan.LoanRestructurings.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>PENDING_APPROVAL -> DRAFT, so the encoder can fix the issue the checker flagged and resubmit.</summary>
public sealed class RejectLoanRestructuringCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RejectLoanRestructuringCommand, Result<LoanRestructuringResponse>>
{
    public async Task<Result<LoanRestructuringResponse>> HandleAsync(RejectLoanRestructuringCommand command, CancellationToken cancellationToken = default)
    {
        var restructuring = await dbContext.LoanRestructurings
            .Include(r => r.OldLoanAccount).ThenInclude(a => a.Customer)
            .Include(r => r.OldLoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(r => r.NewLoanAccount)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (restructuring is null)
            return Result.Failure<LoanRestructuringResponse>(Error.NotFound("LoanRestructuring.NotFound", $"Loan restructuring with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_RESTRUCTURINGS", FormAction.Approve, restructuring.BranchId, cancellationToken))
            return Result.Failure<LoanRestructuringResponse>(Error.Forbidden("LoanRestructuring.Forbidden", "You do not have permission to reject loan restructurings for this branch."));

        if (restructuring.Status != "PENDING_APPROVAL")
            return Result.Failure<LoanRestructuringResponse>(Error.Validation("LoanRestructuring.NotPendingApproval", "Only a loan restructuring pending approval can be rejected."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "LOAN_RESTRUCTURING" && r.EntityId == restructuring.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<LoanRestructuringResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this loan restructuring."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Reject", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<LoanRestructuringResponse>(decideResult.Error!);

        restructuring.Status = "DRAFT";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_RESTRUCTURING", restructuring.Id.ToString(), restructuring.BranchId, "REJECTED", "ACTIVITY",
                $"rejected this loan restructuring — \"{command.Comments}\"", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanRestructuringResponse>(notifyResult.Error!);

        return Result.Success(LoanRestructuringMapper.ToResponse(restructuring));
    }
}
