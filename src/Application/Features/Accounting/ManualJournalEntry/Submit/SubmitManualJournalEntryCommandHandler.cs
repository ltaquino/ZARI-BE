namespace ZARI.Application.Features.Accounting.ManualJournalEntries.Submit;

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

/// <summary>DRAFT -> PENDING_APPROVAL. Re-checks the balance one more time before it can even reach a checker.</summary>
public sealed class SubmitManualJournalEntryCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<SubmitManualJournalEntryCommand, Result<ManualJournalEntryResponse>>
{
    public async Task<Result<ManualJournalEntryResponse>> HandleAsync(SubmitManualJournalEntryCommand command, CancellationToken cancellationToken = default)
    {
        var entry = await dbContext.ManualJournalEntries
            .Include(e => e.Lines).ThenInclude(l => l.GlAccount)
            .Include(e => e.Lines).ThenInclude(l => l.CostCenter)
            .FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken);

        if (entry is null)
            return Result.Failure<ManualJournalEntryResponse>(Error.NotFound("ManualJournalEntry.NotFound", $"Manual journal entry with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("MANUAL_JOURNAL_ENTRIES", FormAction.Edit, entry.BranchId, cancellationToken))
            return Result.Failure<ManualJournalEntryResponse>(Error.Forbidden("ManualJournalEntry.Forbidden", "You do not have permission to submit manual journal entries for this branch."));

        if (entry.Status != "DRAFT")
            return Result.Failure<ManualJournalEntryResponse>(Error.Validation("ManualJournalEntry.NotDraft", "Only draft manual journal entries can be submitted for approval."));

        if (entry.Lines.Count < 2)
            return Result.Failure<ManualJournalEntryResponse>(Error.Validation("ManualJournalEntry.NoLines", "At least two lines are required before submitting for approval."));

        var totalDebit = Math.Round(entry.Lines.Sum(l => l.DebitAmount), 4);
        var totalCredit = Math.Round(entry.Lines.Sum(l => l.CreditAmount), 4);
        if (totalDebit != totalCredit)
            return Result.Failure<ManualJournalEntryResponse>(Error.Validation("ManualJournalEntry.Unbalanced", $"Entry is unbalanced: debit {totalDebit} vs credit {totalCredit}."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("MANUAL_JOURNAL_ENTRY", entry.Id.ToString(), entry.BranchId, command.RequestedBy, null, null),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<ManualJournalEntryResponse>(submitResult.Error!);

        entry.Status = "PENDING_APPROVAL";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("MANUAL_JOURNAL_ENTRY", entry.Id.ToString(), entry.BranchId, "SUBMITTED", "APPROVAL_NEEDED",
                "submitted this manual journal entry for approval", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<ManualJournalEntryResponse>(notifyResult.Error!);

        return Result.Success(ManualJournalEntryMapper.ToResponse(entry));
    }
}
