namespace ZARI.Application.Features.Accounting.ManualJournalEntries.RequestCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.ManualJournalEntries.GetAll;
using ZARI.Application.Features.Accounting.ManualJournalEntries.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>POSTED -> PENDING_CANCELLATION. A same-branch manager flags it; only an HQ admin can finish the cancellation.</summary>
public sealed class RequestManualJournalEntryCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RequestManualJournalEntryCancellationCommand, Result<ManualJournalEntryResponse>>
{
    public async Task<Result<ManualJournalEntryResponse>> HandleAsync(RequestManualJournalEntryCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var entry = await dbContext.ManualJournalEntries
            .Include(e => e.Lines).ThenInclude(l => l.GlAccount)
            .Include(e => e.Lines).ThenInclude(l => l.CostCenter)
            .FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken);

        if (entry is null)
            return Result.Failure<ManualJournalEntryResponse>(Error.NotFound("ManualJournalEntry.NotFound", $"Manual journal entry with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("MANUAL_JOURNAL_ENTRIES", FormAction.Cancel, entry.BranchId, cancellationToken))
            return Result.Failure<ManualJournalEntryResponse>(Error.Forbidden("ManualJournalEntry.Forbidden", "You do not have permission to request cancellation of manual journal entries for this branch."));

        if (entry.Status != "POSTED")
            return Result.Failure<ManualJournalEntryResponse>(Error.Validation("ManualJournalEntry.NotPosted", "Only a posted manual journal entry can have its cancellation requested."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("MANUAL_JOURNAL_ENTRY", entry.Id.ToString(), entry.BranchId, command.RequestedBy, "CANCEL", command.Reason),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<ManualJournalEntryResponse>(submitResult.Error!);

        entry.Status = "PENDING_CANCELLATION";
        entry.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("MANUAL_JOURNAL_ENTRY", entry.Id.ToString(), entry.BranchId, "CANCELLATION_REQUESTED", "APPROVAL_NEEDED",
                $"requested cancellation — \"{command.Reason}\"", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<ManualJournalEntryResponse>(notifyResult.Error!);

        return Result.Success(ManualJournalEntryMapper.ToResponse(entry));
    }
}
