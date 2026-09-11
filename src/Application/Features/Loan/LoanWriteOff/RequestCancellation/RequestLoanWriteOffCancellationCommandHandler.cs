namespace ZARI.Application.Features.Loan.LoanWriteOffs.RequestCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanWriteOffs.GetAll;
using ZARI.Application.Features.Loan.LoanWriteOffs.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// POSTED -> PENDING_CANCELLATION. A same-branch manager flags it; only an HQ admin can finish the
/// cancellation. No "already has downstream activity" guard is needed here — once WRITTEN_OFF, the
/// account can no longer receive a LoanPayment or be Restructured (both require Status == "ACTIVE"),
/// so nothing new can happen to it while a cancellation is pending.
/// </summary>
public sealed class RequestLoanWriteOffCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RequestLoanWriteOffCancellationCommand, Result<LoanWriteOffResponse>>
{
    public async Task<Result<LoanWriteOffResponse>> HandleAsync(RequestLoanWriteOffCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var writeOff = await dbContext.LoanWriteOffs
            .Include(w => w.LoanAccount).ThenInclude(a => a.Customer)
            .Include(w => w.LoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(w => w.WriteOffExpenseAccount)
            .FirstOrDefaultAsync(w => w.Id == command.Id, cancellationToken);

        if (writeOff is null)
            return Result.Failure<LoanWriteOffResponse>(Error.NotFound("LoanWriteOff.NotFound", $"Loan write-off with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_WRITE_OFFS", FormAction.Cancel, writeOff.BranchId, cancellationToken))
            return Result.Failure<LoanWriteOffResponse>(Error.Forbidden("LoanWriteOff.Forbidden", "You do not have permission to request cancellation of loan write-offs for this branch."));

        if (writeOff.Status != "POSTED")
            return Result.Failure<LoanWriteOffResponse>(Error.Validation("LoanWriteOff.NotPosted", "Only a posted loan write-off can have its cancellation requested."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("LOAN_WRITE_OFF", writeOff.Id.ToString(), writeOff.BranchId, command.RequestedBy, "CANCEL", command.Reason),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<LoanWriteOffResponse>(submitResult.Error!);

        writeOff.Status = "PENDING_CANCELLATION";
        writeOff.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_WRITE_OFF", writeOff.Id.ToString(), writeOff.BranchId, "CANCELLATION_REQUESTED", "APPROVAL_NEEDED",
                $"requested cancellation — \"{command.Reason}\"", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanWriteOffResponse>(notifyResult.Error!);

        return Result.Success(LoanWriteOffMapper.ToResponse(writeOff));
    }
}
