namespace ZARI.Application.Features.Loan.LoanAccounts.Cancel;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanAccounts.GetAll;
using ZARI.Application.Features.Loan.LoanAccounts.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_DISBURSEMENT -> CANCELLED only — no Submit/Approve lifecycle on LoanAccount itself (that
/// lives on LoanApplication and, later, LoanDisbursement), so there is no ApprovalRequest to cancel
/// here, unlike CancelLoanApplicationCommandHandler. Once ACTIVE, a loan is closed out through
/// full repayment, restructuring or write-off instead — not a plain cancel.
/// </summary>
public sealed class CancelLoanAccountCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CancelLoanAccountCommand, Result<LoanAccountResponse>>
{
    public async Task<Result<LoanAccountResponse>> HandleAsync(CancelLoanAccountCommand command, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.LoanAccounts
            .Include(a => a.Customer)
            .Include(a => a.LoanProduct)
            .Include(a => a.LoanApplication)
            .Include(a => a.ScheduleLines)
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken);

        if (account is null)
            return Result.Failure<LoanAccountResponse>(Error.NotFound("LoanAccount.NotFound", $"Loan account with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_ACCOUNTS", FormAction.Cancel, account.BranchId, cancellationToken))
            return Result.Failure<LoanAccountResponse>(Error.Forbidden("LoanAccount.Forbidden", "You do not have permission to cancel this loan account for this branch."));

        if (account.Status != "PENDING_DISBURSEMENT")
            return Result.Failure<LoanAccountResponse>(Error.Validation("LoanAccount.NotCancellable", "Only a loan account still pending disbursement can be cancelled."));

        account.Status = "CANCELLED";
        account.CancelledBy = command.CancelledBy;
        account.CancelledAt = DateTimeOffset.UtcNow;
        account.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_ACCOUNT", account.Id.ToString(), account.BranchId, "CANCELLED", "ACTIVITY",
                $"cancelled this loan account — \"{command.Reason}\"", command.CancelledBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanAccountResponse>(notifyResult.Error!);

        return Result.Success(LoanAccountMapper.ToResponse(account));
    }
}
