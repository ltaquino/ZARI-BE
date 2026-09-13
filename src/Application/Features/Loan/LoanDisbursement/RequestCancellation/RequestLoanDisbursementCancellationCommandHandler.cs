namespace ZARI.Application.Features.Loan.LoanDisbursements.RequestCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanDisbursements.GetAll;
using ZARI.Application.Features.Loan.LoanDisbursements.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// POSTED -> PENDING_CANCELLATION. A same-branch manager flags it; only an HQ admin can finish the
/// cancellation. Blocks the request if the loan account already has a non-cancelled LoanPayment —
/// same "already has downstream activity" guard as GoodsReceiptPo's AP Invoice/Goods Return check —
/// since reversing the disbursement out from under a schedule that's already been partly paid down
/// would corrupt the payment's own allocations. Mirrored in
/// ApproveLoanDisbursementCancellationCommandHandler.
/// </summary>
public sealed class RequestLoanDisbursementCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RequestLoanDisbursementCancellationCommand, Result<LoanDisbursementResponse>>
{
    public async Task<Result<LoanDisbursementResponse>> HandleAsync(RequestLoanDisbursementCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var disbursement = await dbContext.LoanDisbursements
            .Include(d => d.LoanAccount).ThenInclude(a => a.Customer)
            .Include(d => d.LoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(d => d.PaymentMethod)
            .Include(d => d.CostCenter)
            .FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken);

        if (disbursement is null)
            return Result.Failure<LoanDisbursementResponse>(Error.NotFound("LoanDisbursement.NotFound", $"Loan disbursement with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_DISBURSEMENTS", FormAction.Cancel, disbursement.BranchId, cancellationToken))
            return Result.Failure<LoanDisbursementResponse>(Error.Forbidden("LoanDisbursement.Forbidden", "You do not have permission to request cancellation of loan disbursements for this branch."));

        if (disbursement.Status != "POSTED")
            return Result.Failure<LoanDisbursementResponse>(Error.Validation("LoanDisbursement.NotPosted", "Only a posted loan disbursement can have its cancellation requested."));

        var hasPayments = await dbContext.LoanPayments.AnyAsync(p => p.LoanAccountId == disbursement.LoanAccountId && p.Status != "CANCELLED", cancellationToken);
        if (hasPayments)
            return Result.Failure<LoanDisbursementResponse>(Error.Validation("LoanDisbursement.HasDownstreamActivity", "This loan account already has a payment posted against it — cancel or reverse those first."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("LOAN_DISBURSEMENT", disbursement.Id.ToString(), disbursement.BranchId, command.RequestedBy, "CANCEL", command.Reason),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<LoanDisbursementResponse>(submitResult.Error!);

        disbursement.Status = "PENDING_CANCELLATION";
        disbursement.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_DISBURSEMENT", disbursement.Id.ToString(), disbursement.BranchId, "CANCELLATION_REQUESTED", "APPROVAL_NEEDED",
                $"requested cancellation — \"{command.Reason}\"", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanDisbursementResponse>(notifyResult.Error!);

        return Result.Success(LoanDisbursementMapper.ToResponse(disbursement));
    }
}
