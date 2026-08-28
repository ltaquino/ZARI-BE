namespace ZARI.Application.Features.Accounting.ManualJournalEntries.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.ManualJournalEntries.GetAll;
using ZARI.Application.Features.Accounting.ManualJournalEntries.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>DRAFT-only edit. BranchId is immutable once set (the doc number and permission-branch already depend on it).</summary>
public sealed class UpdateManualJournalEntryCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<UpdateManualJournalEntryCommand, Result<ManualJournalEntryResponse>>
{
    public async Task<Result<ManualJournalEntryResponse>> HandleAsync(UpdateManualJournalEntryCommand command, CancellationToken cancellationToken = default)
    {
        var entry = await dbContext.ManualJournalEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken);

        if (entry is null)
            return Result.Failure<ManualJournalEntryResponse>(Error.NotFound("ManualJournalEntry.NotFound", $"Manual journal entry with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("MANUAL_JOURNAL_ENTRIES", FormAction.Edit, entry.BranchId, cancellationToken))
            return Result.Failure<ManualJournalEntryResponse>(Error.Forbidden("ManualJournalEntry.Forbidden", "You do not have permission to update manual journal entries for this branch."));

        if (entry.Status != "DRAFT")
            return Result.Failure<ManualJournalEntryResponse>(Error.Validation("ManualJournalEntry.NotDraft", "Only draft manual journal entries can be edited."));

        var glAccountIds = command.Lines.Select(l => l.GlAccountId).Distinct().ToList();
        var glAccounts = await dbContext.GlAccounts.Where(a => glAccountIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, cancellationToken);
        if (glAccounts.Count != glAccountIds.Count)
            return Result.Failure<ManualJournalEntryResponse>(Error.NotFound("GlAccount.NotFound", "One or more GL accounts on this entry were not found."));

        var costCenterIds = command.Lines.Where(l => l.CostCenterId.HasValue).Select(l => l.CostCenterId!.Value).Distinct().ToList();
        var costCenters = await dbContext.CostCenters.Where(c => costCenterIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, cancellationToken);
        if (costCenters.Count != costCenterIds.Count)
            return Result.Failure<ManualJournalEntryResponse>(Error.NotFound("CostCenter.NotFound", "One or more cost centers on this entry were not found."));

        entry.EntryDate = command.EntryDate;
        entry.Remarks = command.Remarks;

        entry.Lines.Clear();
        foreach (var line in command.Lines)
        {
            entry.Lines.Add(new ManualJournalEntryLine
            {
                GlAccountId = line.GlAccountId,
                CostCenterId = line.CostCenterId,
                Memo = line.Memo,
                DebitAmount = line.DebitAmount,
                CreditAmount = line.CreditAmount
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var line in entry.Lines)
        {
            line.GlAccount = glAccounts[line.GlAccountId];
            if (line.CostCenterId.HasValue) line.CostCenter = costCenters[line.CostCenterId.Value];
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("MANUAL_JOURNAL_ENTRY", entry.Id.ToString(), entry.BranchId, "UPDATED", "ACTIVITY",
                "updated this manual journal entry", command.UpdatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<ManualJournalEntryResponse>(notifyResult.Error!);

        return Result.Success(ManualJournalEntryMapper.ToResponse(entry));
    }
}
