namespace ZARI.Application.Features.Loan.LoanWriteOffs.Reject;

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

/// <summary>
/// PENDING_APPROVAL -> DRAFT, so the encoder can fix the issue the approving HQ admin flagged and
/// resubmit. Rejecting uses the same elevated authority as approving — mirrors
/// RejectLoanRestructuringCommandHandler's use of FormAction.Approve, just via the HQ-only check.
/// </summary>
public sealed class RejectLoanWriteOffCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RejectLoanWriteOffCommand, Result<LoanWriteOffResponse>>
{
    public async Task<Result<LoanWriteOffResponse>> HandleAsync(RejectLoanWriteOffCommand command, CancellationToken cancellationToken = default)
    {
        var writeOff = await dbContext.LoanWriteOffs
            .Include(w => w.LoanAccount).ThenInclude(a => a.Customer)
            .Include(w => w.LoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(w => w.WriteOffExpenseAccount)
            .FirstOrDefaultAsync(w => w.Id == command.Id, cancellationToken);

        if (writeOff is null)
            return Result.Failure<LoanWriteOffResponse>(Error.NotFound("LoanWriteOff.NotFound", $"Loan write-off with ID '{command.Id}' was not found."));

        if (!await permissionService.HasHqApprovalAuthorityAsync("LOAN_WRITE_OFFS", cancellationToken))
            return Result.Failure<LoanWriteOffResponse>(Error.Forbidden("LoanWriteOff.Forbidden", "Only someone with approve permission assigned to the head office branch can decide a loan write-off."));

        if (writeOff.Status != "PENDING_APPROVAL")
            return Result.Failure<LoanWriteOffResponse>(Error.Validation("LoanWriteOff.NotPendingApproval", "Only a loan write-off pending approval can be rejected."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "LOAN_WRITE_OFF" && r.EntityId == writeOff.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<LoanWriteOffResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this loan write-off."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Reject", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<LoanWriteOffResponse>(decideResult.Error!);

        writeOff.Status = "DRAFT";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_WRITE_OFF", writeOff.Id.ToString(), writeOff.BranchId, "REJECTED", "ACTIVITY",
                $"rejected this loan write-off — \"{command.Comments}\"", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanWriteOffResponse>(notifyResult.Error!);

        return Result.Success(LoanWriteOffMapper.ToResponse(writeOff));
    }
}
