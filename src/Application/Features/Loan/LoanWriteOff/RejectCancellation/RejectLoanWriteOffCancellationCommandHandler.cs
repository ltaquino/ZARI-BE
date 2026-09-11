namespace ZARI.Application.Features.Loan.LoanWriteOffs.RejectCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanWriteOffs.GetAll;
using ZARI.Application.Features.Loan.LoanWriteOffs.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>PENDING_CANCELLATION -> POSTED. The HQ admin declines the request; the write-off stands as posted.</summary>
public sealed class RejectLoanWriteOffCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RejectLoanWriteOffCancellationCommand, Result<LoanWriteOffResponse>>
{
    public async Task<Result<LoanWriteOffResponse>> HandleAsync(RejectLoanWriteOffCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var writeOff = await dbContext.LoanWriteOffs
            .Include(w => w.LoanAccount).ThenInclude(a => a.Customer)
            .Include(w => w.LoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(w => w.WriteOffExpenseAccount)
            .FirstOrDefaultAsync(w => w.Id == command.Id, cancellationToken);

        if (writeOff is null)
            return Result.Failure<LoanWriteOffResponse>(Error.NotFound("LoanWriteOff.NotFound", $"Loan write-off with ID '{command.Id}' was not found."));

        if (!await permissionService.HasCancellationAuthorityAsync("LOAN_WRITE_OFFS", cancellationToken))
            return Result.Failure<LoanWriteOffResponse>(Error.Forbidden("LoanWriteOff.Forbidden", "Only someone with cancel permission assigned to the head office branch can decide a cancellation request."));

        if (writeOff.Status != "PENDING_CANCELLATION")
            return Result.Failure<LoanWriteOffResponse>(Error.Validation("LoanWriteOff.NotPendingCancellation", "Only a loan write-off pending cancellation can have that request rejected."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "LOAN_WRITE_OFF" && r.EntityId == writeOff.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<LoanWriteOffResponse>(Error.NotFound("ApprovalRequest.NotFound", "No cancellation request found for this loan write-off."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Reject", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<LoanWriteOffResponse>(decideResult.Error!);

        writeOff.Status = "POSTED";
        writeOff.CancelReason = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_WRITE_OFF", writeOff.Id.ToString(), writeOff.BranchId, "CANCELLATION_REJECTED", "ACTIVITY",
                $"declined the cancellation request — \"{command.Comments}\"", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanWriteOffResponse>(notifyResult.Error!);

        return Result.Success(LoanWriteOffMapper.ToResponse(writeOff));
    }
}
