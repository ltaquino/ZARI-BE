namespace ZARI.Application.Features.Loan.LoanPayments.RequestCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanPayments.GetAll;
using ZARI.Application.Features.Loan.LoanPayments.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// POSTED -> PENDING_CANCELLATION. Every LoanPayment is POSTED the moment it exists (see the
/// entity's own class doc comment), so — unlike LoanDisbursement — there's no pre-post Cancel tier
/// at all here; this is the only cancellation entry point.
/// </summary>
public sealed class RequestLoanPaymentCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RequestLoanPaymentCancellationCommand, Result<LoanPaymentResponse>>
{
    public async Task<Result<LoanPaymentResponse>> HandleAsync(RequestLoanPaymentCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.LoanPayments
            .Include(p => p.LoanAccount).ThenInclude(a => a.Customer)
            .Include(p => p.LoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(p => p.PaymentMethod)
            .Include(p => p.CostCenter)
            .Include(p => p.Allocations).ThenInclude(a => a.LoanAmortizationScheduleLine)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (payment is null)
            return Result.Failure<LoanPaymentResponse>(Error.NotFound("LoanPayment.NotFound", $"Loan payment with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_PAYMENTS", FormAction.Cancel, payment.BranchId, cancellationToken))
            return Result.Failure<LoanPaymentResponse>(Error.Forbidden("LoanPayment.Forbidden", "You do not have permission to request cancellation of loan payments for this branch."));

        if (payment.Status != "POSTED")
            return Result.Failure<LoanPaymentResponse>(Error.Validation("LoanPayment.NotPosted", "Only a posted loan payment can have its cancellation requested."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("LOAN_PAYMENT", payment.Id.ToString(), payment.BranchId, command.RequestedBy, "CANCEL", command.Reason),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<LoanPaymentResponse>(submitResult.Error!);

        payment.Status = "PENDING_CANCELLATION";
        payment.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_PAYMENT", payment.Id.ToString(), payment.BranchId, "CANCELLATION_REQUESTED", "APPROVAL_NEEDED",
                $"requested cancellation — \"{command.Reason}\"", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanPaymentResponse>(notifyResult.Error!);

        return Result.Success(LoanPaymentMapper.ToResponse(payment));
    }
}
