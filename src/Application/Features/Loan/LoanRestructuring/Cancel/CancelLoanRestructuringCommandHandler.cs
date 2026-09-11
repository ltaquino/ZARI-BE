namespace ZARI.Application.Features.Loan.LoanRestructurings.Cancel;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanRestructurings.GetAll;
using ZARI.Application.Features.Loan.LoanRestructurings.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// Direct cancel for DRAFT / PENDING_APPROVAL only — nothing's posted yet, so no reversal is
/// needed. A POSTED restructuring has to go through RequestLoanRestructuringCancellation instead.
/// </summary>
public sealed class CancelLoanRestructuringCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CancelPendingApprovalRequestCommand, Result> cancelPendingHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CancelLoanRestructuringCommand, Result<LoanRestructuringResponse>>
{
    public async Task<Result<LoanRestructuringResponse>> HandleAsync(CancelLoanRestructuringCommand command, CancellationToken cancellationToken = default)
    {
        var restructuring = await dbContext.LoanRestructurings
            .Include(r => r.OldLoanAccount).ThenInclude(a => a.Customer)
            .Include(r => r.OldLoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(r => r.NewLoanAccount)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (restructuring is null)
            return Result.Failure<LoanRestructuringResponse>(Error.NotFound("LoanRestructuring.NotFound", $"Loan restructuring with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_RESTRUCTURINGS", FormAction.Cancel, restructuring.BranchId, cancellationToken))
            return Result.Failure<LoanRestructuringResponse>(Error.Forbidden("LoanRestructuring.Forbidden", "You do not have permission to cancel this loan restructuring for this branch."));

        if (restructuring.Status == "CANCELLED")
            return Result.Failure<LoanRestructuringResponse>(Error.Validation("LoanRestructuring.AlreadyCancelled", "This loan restructuring is already cancelled."));

        if (restructuring.Status is "POSTED" or "PENDING_CANCELLATION")
            return Result.Failure<LoanRestructuringResponse>(Error.Validation("LoanRestructuring.RequiresCancellationRequest", "A posted loan restructuring must go through a cancellation request instead."));

        var cancelPendingResult = await cancelPendingHandler.HandleAsync(new CancelPendingApprovalRequestCommand("LOAN_RESTRUCTURING", restructuring.Id.ToString()), cancellationToken);
        if (!cancelPendingResult.IsSuccess)
            return Result.Failure<LoanRestructuringResponse>(cancelPendingResult.Error!);

        restructuring.Status = "CANCELLED";
        restructuring.CancelledBy = command.CancelledBy;
        restructuring.CancelledAt = DateTimeOffset.UtcNow;
        restructuring.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_RESTRUCTURING", restructuring.Id.ToString(), restructuring.BranchId, "CANCELLED", "ACTIVITY",
                $"cancelled this loan restructuring — \"{command.Reason}\"", command.CancelledBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanRestructuringResponse>(notifyResult.Error!);

        return Result.Success(LoanRestructuringMapper.ToResponse(restructuring));
    }
}
