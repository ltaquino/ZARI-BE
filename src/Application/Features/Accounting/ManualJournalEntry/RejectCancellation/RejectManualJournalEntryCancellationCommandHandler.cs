namespace ZARI.Application.Features.Accounting.ManualJournalEntries.RejectCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.ManualJournalEntries.GetAll;
using ZARI.Application.Features.Accounting.ManualJournalEntries.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>PENDING_CANCELLATION -> POSTED. The HQ admin declines the request; the entry stands as posted.</summary>
public sealed class RejectManualJournalEntryCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RejectManualJournalEntryCancellationCommand, Result<ManualJournalEntryResponse>>
{
    public async Task<Result<ManualJournalEntryResponse>> HandleAsync(RejectManualJournalEntryCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var entry = await dbContext.ManualJournalEntries
            .Include(e => e.Lines).ThenInclude(l => l.GlAccount)
            .Include(e => e.Lines).ThenInclude(l => l.CostCenter)
            .FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken);

        if (entry is null)
            return Result.Failure<ManualJournalEntryResponse>(Error.NotFound("ManualJournalEntry.NotFound", $"Manual journal entry with ID '{command.Id}' was not found."));

        if (!await permissionService.HasCancellationAuthorityAsync("MANUAL_JOURNAL_ENTRIES", cancellationToken))
            return Result.Failure<ManualJournalEntryResponse>(Error.Forbidden("ManualJournalEntry.Forbidden", "Only someone with cancel permission assigned to the head office branch can decide a cancellation request."));

        if (entry.Status != "PENDING_CANCELLATION")
            return Result.Failure<ManualJournalEntryResponse>(Error.Validation("ManualJournalEntry.NotPendingCancellation", "Only a manual journal entry pending cancellation can have that request rejected."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "MANUAL_JOURNAL_ENTRY" && r.EntityId == entry.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<ManualJournalEntryResponse>(Error.NotFound("ApprovalRequest.NotFound", "No cancellation request found for this manual journal entry."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Reject", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<ManualJournalEntryResponse>(decideResult.Error!);

        entry.Status = "POSTED";
        entry.CancelReason = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("MANUAL_JOURNAL_ENTRY", entry.Id.ToString(), entry.BranchId, "CANCELLATION_REJECTED", "ACTIVITY",
                $"declined the cancellation request — \"{command.Comments}\"", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<ManualJournalEntryResponse>(notifyResult.Error!);

        return Result.Success(ManualJournalEntryMapper.ToResponse(entry));
    }
}
