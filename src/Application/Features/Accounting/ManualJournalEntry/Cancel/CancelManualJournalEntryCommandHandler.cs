namespace ZARI.Application.Features.Accounting.ManualJournalEntries.Cancel;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.ManualJournalEntries.GetAll;
using ZARI.Application.Features.Accounting.ManualJournalEntries.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// Direct cancel for DRAFT / PENDING_APPROVAL only — nothing's posted yet, so no reversal is
/// needed. A POSTED entry has to go through RequestManualJournalEntryCancellation instead.
/// </summary>
public sealed class CancelManualJournalEntryCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CancelPendingApprovalRequestCommand, Result> cancelPendingHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CancelManualJournalEntryCommand, Result<ManualJournalEntryResponse>>
{
    public async Task<Result<ManualJournalEntryResponse>> HandleAsync(CancelManualJournalEntryCommand command, CancellationToken cancellationToken = default)
    {
        var entry = await dbContext.ManualJournalEntries
            .Include(e => e.Lines).ThenInclude(l => l.GlAccount)
            .Include(e => e.Lines).ThenInclude(l => l.CostCenter)
            .FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken);

        if (entry is null)
            return Result.Failure<ManualJournalEntryResponse>(Error.NotFound("ManualJournalEntry.NotFound", $"Manual journal entry with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("MANUAL_JOURNAL_ENTRIES", FormAction.Cancel, entry.BranchId, cancellationToken))
            return Result.Failure<ManualJournalEntryResponse>(Error.Forbidden("ManualJournalEntry.Forbidden", "You do not have permission to cancel manual journal entries for this branch."));

        if (entry.Status == "CANCELLED")
            return Result.Failure<ManualJournalEntryResponse>(Error.Validation("ManualJournalEntry.AlreadyCancelled", "This manual journal entry is already cancelled."));

        if (entry.Status is "POSTED" or "PENDING_CANCELLATION")
            return Result.Failure<ManualJournalEntryResponse>(Error.Validation("ManualJournalEntry.RequiresCancellationRequest", "A posted manual journal entry must go through a cancellation request instead."));

        var cancelPendingResult = await cancelPendingHandler.HandleAsync(new CancelPendingApprovalRequestCommand("MANUAL_JOURNAL_ENTRY", entry.Id.ToString()), cancellationToken);
        if (!cancelPendingResult.IsSuccess)
            return Result.Failure<ManualJournalEntryResponse>(cancelPendingResult.Error!);

        entry.Status = "CANCELLED";
        entry.CancelledBy = command.CancelledBy;
        entry.CancelledAt = DateTimeOffset.UtcNow;
        entry.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("MANUAL_JOURNAL_ENTRY", entry.Id.ToString(), entry.BranchId, "CANCELLED", "ACTIVITY",
                $"cancelled this manual journal entry — \"{command.Reason}\"", command.CancelledBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<ManualJournalEntryResponse>(notifyResult.Error!);

        return Result.Success(ManualJournalEntryMapper.ToResponse(entry));
    }
}
