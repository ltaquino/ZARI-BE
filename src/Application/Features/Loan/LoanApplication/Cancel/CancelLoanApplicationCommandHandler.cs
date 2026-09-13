namespace ZARI.Application.Features.Loan.LoanApplications.Cancel;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Loan.LoanApplications.GetAll;
using ZARI.Application.Features.Loan.LoanApplications.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// DRAFT / PENDING_APPROVAL / APPROVED -> CANCELLED. No posted-document two-tier flow needed —
/// this table has zero stock/GL impact, so a single-tier cancel is enough (same as PurchaseRequest).
/// </summary>
public sealed class CancelLoanApplicationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CancelPendingApprovalRequestCommand, Result> cancelPendingHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CancelLoanApplicationCommand, Result<LoanApplicationResponse>>
{
    public async Task<Result<LoanApplicationResponse>> HandleAsync(CancelLoanApplicationCommand command, CancellationToken cancellationToken = default)
    {
        var application = await dbContext.LoanApplications
            .Include(a => a.Customer)
            .Include(a => a.LoanProduct)
            .Include(a => a.Collaterals)
            .Include(a => a.CoMakers).ThenInclude(c => c.CoMakerCustomer)
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken);

        if (application is null)
            return Result.Failure<LoanApplicationResponse>(Error.NotFound("LoanApplication.NotFound", $"Loan application with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_APPLICATIONS", FormAction.Cancel, application.BranchId, cancellationToken))
            return Result.Failure<LoanApplicationResponse>(Error.Forbidden("LoanApplication.Forbidden", "You do not have permission to cancel this loan application for this branch."));

        if (application.Status == "CANCELLED")
            return Result.Failure<LoanApplicationResponse>(Error.Validation("LoanApplication.AlreadyCancelled", "This loan application is already cancelled."));

        var cancelPendingResult = await cancelPendingHandler.HandleAsync(new CancelPendingApprovalRequestCommand("LOAN_APPLICATION", application.Id.ToString()), cancellationToken);
        if (!cancelPendingResult.IsSuccess)
            return Result.Failure<LoanApplicationResponse>(cancelPendingResult.Error!);

        application.Status = "CANCELLED";
        application.CancelledBy = command.CancelledBy;
        application.CancelledAt = DateTimeOffset.UtcNow;
        application.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_APPLICATION", application.Id.ToString(), application.BranchId, "CANCELLED", "ACTIVITY",
                $"cancelled this loan application — \"{command.Reason}\"", command.CancelledBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanApplicationResponse>(notifyResult.Error!);

        return Result.Success(LoanApplicationMapper.ToResponse(application));
    }
}
