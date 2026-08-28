namespace ZARI.Application.Features.Accounting.ManualJournalEntries.ApproveCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Reverse;
using ZARI.Application.Features.Accounting.ManualJournalEntries.GetAll;
using ZARI.Application.Features.Accounting.ManualJournalEntries.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_CANCELLATION -> CANCELLED. Only an HQ admin may finalize the reversal of a posted
/// manual journal entry — reverses the posted GL journal, then decides the cancellation request.
/// </summary>
public sealed class ApproveManualJournalEntryCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<ReverseGlJournalsCommand, Result<List<GlJournalResponse>>> reverseGlJournalsHandler,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveManualJournalEntryCancellationCommand, Result<ManualJournalEntryResponse>>
{
    public async Task<Result<ManualJournalEntryResponse>> HandleAsync(ApproveManualJournalEntryCancellationCommand command, CancellationToken cancellationToken = default)
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
            return Result.Failure<ManualJournalEntryResponse>(Error.Validation("ManualJournalEntry.NotPendingCancellation", "Only a manual journal entry pending cancellation can be cancelled this way."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "MANUAL_JOURNAL_ENTRY" && r.EntityId == entry.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<ManualJournalEntryResponse>(Error.NotFound("ApprovalRequest.NotFound", "No cancellation request found for this manual journal entry."));

        var reverseJournalsResult = await reverseGlJournalsHandler.HandleAsync(
            new ReverseGlJournalsCommand("ManualJournalEntry", entry.Id.ToString(), DateTimeOffset.UtcNow, $"Cancellation of {entry.EntryNo}"), cancellationToken);
        if (!reverseJournalsResult.IsSuccess)
            return Result.Failure<ManualJournalEntryResponse>(reverseJournalsResult.Error!);

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<ManualJournalEntryResponse>(decideResult.Error!);

        entry.Status = "CANCELLED";
        entry.CancelledBy = command.ApproverUserId;
        entry.CancelledAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("MANUAL_JOURNAL_ENTRY", entry.Id.ToString(), entry.BranchId, "CANCELLATION_APPROVED", "ACTIVITY",
                "approved the cancellation request", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<ManualJournalEntryResponse>(notifyResult.Error!);

        return Result.Success(ManualJournalEntryMapper.ToResponse(entry));
    }
}
