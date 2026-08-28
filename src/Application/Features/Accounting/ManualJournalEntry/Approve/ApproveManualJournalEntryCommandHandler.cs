namespace ZARI.Application.Features.Accounting.ManualJournalEntries.Approve;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Accounting.ManualJournalEntries.GetAll;
using ZARI.Application.Features.Accounting.ManualJournalEntries.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_APPROVAL -> POSTED. Posts the entry's own lines straight through as a real GlJournal —
/// no derivation needed, unlike every other module's Approve handler, since the user already typed
/// exactly the debit/credit lines they want. PostGlJournalCommandHandler is still the final,
/// authoritative balance check (ManualJournalEntry.Unbalanced at Submit time is a friendlier
/// earlier check, not a replacement for it).
/// </summary>
public sealed class ApproveManualJournalEntryCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveManualJournalEntryCommand, Result<ManualJournalEntryResponse>>
{
    public async Task<Result<ManualJournalEntryResponse>> HandleAsync(ApproveManualJournalEntryCommand command, CancellationToken cancellationToken = default)
    {
        var entry = await dbContext.ManualJournalEntries
            .Include(e => e.Lines).ThenInclude(l => l.GlAccount)
            .Include(e => e.Lines).ThenInclude(l => l.CostCenter)
            .FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken);

        if (entry is null)
            return Result.Failure<ManualJournalEntryResponse>(Error.NotFound("ManualJournalEntry.NotFound", $"Manual journal entry with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("MANUAL_JOURNAL_ENTRIES", FormAction.Approve, entry.BranchId, cancellationToken))
            return Result.Failure<ManualJournalEntryResponse>(Error.Forbidden("ManualJournalEntry.Forbidden", "You do not have permission to approve manual journal entries for this branch."));

        if (entry.Status != "PENDING_APPROVAL")
            return Result.Failure<ManualJournalEntryResponse>(Error.Validation("ManualJournalEntry.NotPendingApproval", "Only manual journal entries pending approval can be approved."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "MANUAL_JOURNAL_ENTRY" && r.EntityId == entry.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<ManualJournalEntryResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this manual journal entry."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<ManualJournalEntryResponse>(decideResult.Error!);

        var lines = entry.Lines
            .Select(l => new PostGlJournalLineInput(l.GlAccountId, l.CostCenterId, l.DebitAmount, l.CreditAmount, l.Memo))
            .ToList();

        var postResult = await postGlJournalHandler.HandleAsync(
            new PostGlJournalCommand(entry.BranchId, entry.EntryDate, "ACCOUNTING", "ManualJournalEntry", entry.Id.ToString(), entry.Remarks, lines),
            cancellationToken);
        if (!postResult.IsSuccess)
            return Result.Failure<ManualJournalEntryResponse>(postResult.Error!);

        entry.Status = "POSTED";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("MANUAL_JOURNAL_ENTRY", entry.Id.ToString(), entry.BranchId, "APPROVED", "ACTIVITY",
                "approved this manual journal entry", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<ManualJournalEntryResponse>(notifyResult.Error!);

        return Result.Success(ManualJournalEntryMapper.ToResponse(entry));
    }
}
