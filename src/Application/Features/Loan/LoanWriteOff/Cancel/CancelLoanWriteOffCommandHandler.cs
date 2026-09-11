namespace ZARI.Application.Features.Loan.LoanWriteOffs.Cancel;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanWriteOffs.GetAll;
using ZARI.Application.Features.Loan.LoanWriteOffs.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// Direct cancel for DRAFT / PENDING_APPROVAL only — nothing's posted yet, so no reversal is
/// needed. A POSTED write-off has to go through RequestLoanWriteOffCancellation instead.
/// </summary>
public sealed class CancelLoanWriteOffCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CancelPendingApprovalRequestCommand, Result> cancelPendingHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CancelLoanWriteOffCommand, Result<LoanWriteOffResponse>>
{
    public async Task<Result<LoanWriteOffResponse>> HandleAsync(CancelLoanWriteOffCommand command, CancellationToken cancellationToken = default)
    {
        var writeOff = await dbContext.LoanWriteOffs
            .Include(w => w.LoanAccount).ThenInclude(a => a.Customer)
            .Include(w => w.LoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(w => w.WriteOffExpenseAccount)
            .FirstOrDefaultAsync(w => w.Id == command.Id, cancellationToken);

        if (writeOff is null)
            return Result.Failure<LoanWriteOffResponse>(Error.NotFound("LoanWriteOff.NotFound", $"Loan write-off with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_WRITE_OFFS", FormAction.Cancel, writeOff.BranchId, cancellationToken))
            return Result.Failure<LoanWriteOffResponse>(Error.Forbidden("LoanWriteOff.Forbidden", "You do not have permission to cancel this loan write-off for this branch."));

        if (writeOff.Status == "CANCELLED")
            return Result.Failure<LoanWriteOffResponse>(Error.Validation("LoanWriteOff.AlreadyCancelled", "This loan write-off is already cancelled."));

        if (writeOff.Status is "POSTED" or "PENDING_CANCELLATION")
            return Result.Failure<LoanWriteOffResponse>(Error.Validation("LoanWriteOff.RequiresCancellationRequest", "A posted loan write-off must go through a cancellation request instead."));

        var cancelPendingResult = await cancelPendingHandler.HandleAsync(new CancelPendingApprovalRequestCommand("LOAN_WRITE_OFF", writeOff.Id.ToString()), cancellationToken);
        if (!cancelPendingResult.IsSuccess)
            return Result.Failure<LoanWriteOffResponse>(cancelPendingResult.Error!);

        writeOff.Status = "CANCELLED";
        writeOff.CancelledBy = command.CancelledBy;
        writeOff.CancelledAt = DateTimeOffset.UtcNow;
        writeOff.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_WRITE_OFF", writeOff.Id.ToString(), writeOff.BranchId, "CANCELLED", "ACTIVITY",
                $"cancelled this loan write-off — \"{command.Reason}\"", command.CancelledBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanWriteOffResponse>(notifyResult.Error!);

        return Result.Success(LoanWriteOffMapper.ToResponse(writeOff));
    }
}
