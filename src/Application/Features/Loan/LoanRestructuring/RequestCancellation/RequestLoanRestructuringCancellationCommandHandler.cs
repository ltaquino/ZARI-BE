namespace ZARI.Application.Features.Loan.LoanRestructurings.RequestCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanRestructurings.GetAll;
using ZARI.Application.Features.Loan.LoanRestructurings.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// POSTED -> PENDING_CANCELLATION. A same-branch manager flags it; only an HQ admin can finish the
/// cancellation. Blocks the request if the spawned new loan account already has a non-cancelled
/// LoanPayment against it — same "already has downstream activity" guard added to LoanDisbursement
/// in build-order step 7, since unwinding the restructuring out from under a schedule that's already
/// been partly paid down would corrupt the payment's own allocations.
/// </summary>
public sealed class RequestLoanRestructuringCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RequestLoanRestructuringCancellationCommand, Result<LoanRestructuringResponse>>
{
    public async Task<Result<LoanRestructuringResponse>> HandleAsync(RequestLoanRestructuringCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var restructuring = await dbContext.LoanRestructurings
            .Include(r => r.OldLoanAccount).ThenInclude(a => a.Customer)
            .Include(r => r.OldLoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(r => r.NewLoanAccount)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (restructuring is null)
            return Result.Failure<LoanRestructuringResponse>(Error.NotFound("LoanRestructuring.NotFound", $"Loan restructuring with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_RESTRUCTURINGS", FormAction.Cancel, restructuring.BranchId, cancellationToken))
            return Result.Failure<LoanRestructuringResponse>(Error.Forbidden("LoanRestructuring.Forbidden", "You do not have permission to request cancellation of loan restructurings for this branch."));

        if (restructuring.Status != "POSTED")
            return Result.Failure<LoanRestructuringResponse>(Error.Validation("LoanRestructuring.NotPosted", "Only a posted loan restructuring can have its cancellation requested."));

        var hasPayments = await dbContext.LoanPayments.AnyAsync(p => p.LoanAccountId == restructuring.NewLoanAccountId && p.Status != "CANCELLED", cancellationToken);
        if (hasPayments)
            return Result.Failure<LoanRestructuringResponse>(Error.Validation("LoanRestructuring.HasDownstreamActivity", "The new loan account already has a payment posted against it — cancel or reverse those first."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("LOAN_RESTRUCTURING", restructuring.Id.ToString(), restructuring.BranchId, command.RequestedBy, "CANCEL", command.Reason),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<LoanRestructuringResponse>(submitResult.Error!);

        restructuring.Status = "PENDING_CANCELLATION";
        restructuring.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_RESTRUCTURING", restructuring.Id.ToString(), restructuring.BranchId, "CANCELLATION_REQUESTED", "APPROVAL_NEEDED",
                $"requested cancellation — \"{command.Reason}\"", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanRestructuringResponse>(notifyResult.Error!);

        return Result.Success(LoanRestructuringMapper.ToResponse(restructuring));
    }
}
