namespace ZARI.Application.Features.Accounting.ManualJournalEntries.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.ManualJournalEntries.GetAll;
using ZARI.Application.Features.Accounting.ManualJournalEntries.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateManualJournalEntryCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CreateManualJournalEntryCommand, Result<ManualJournalEntryResponse>>
{
    public async Task<Result<ManualJournalEntryResponse>> HandleAsync(CreateManualJournalEntryCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("MANUAL_JOURNAL_ENTRIES", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<ManualJournalEntryResponse>(Error.Forbidden("ManualJournalEntry.Forbidden", "You do not have permission to create manual journal entries for this branch."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<ManualJournalEntryResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var glAccountIds = command.Lines.Select(l => l.GlAccountId).Distinct().ToList();
        var glAccounts = await dbContext.GlAccounts.Where(a => glAccountIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, cancellationToken);
        if (glAccounts.Count != glAccountIds.Count)
            return Result.Failure<ManualJournalEntryResponse>(Error.NotFound("GlAccount.NotFound", "One or more GL accounts on this entry were not found."));

        var costCenterIds = command.Lines.Where(l => l.CostCenterId.HasValue).Select(l => l.CostCenterId!.Value).Distinct().ToList();
        var costCenters = await dbContext.CostCenters.Where(c => costCenterIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, cancellationToken);
        if (costCenters.Count != costCenterIds.Count)
            return Result.Failure<ManualJournalEntryResponse>(Error.NotFound("CostCenter.NotFound", "One or more cost centers on this entry were not found."));

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(command.BranchId, "MJE"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<ManualJournalEntryResponse>(numberResult.Error!);

        var entry = new ManualJournalEntry
        {
            EntryNo = numberResult.Value!.DocumentNumber,
            BranchId = command.BranchId,
            EntryDate = command.EntryDate,
            Status = "DRAFT",
            Remarks = command.Remarks,
            CreatedBy = command.CreatedBy,
            Lines = command.Lines.Select(l => new ManualJournalEntryLine
            {
                GlAccountId = l.GlAccountId,
                CostCenterId = l.CostCenterId,
                Memo = l.Memo,
                DebitAmount = l.DebitAmount,
                CreditAmount = l.CreditAmount
            }).ToList()
        };

        dbContext.ManualJournalEntries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var line in entry.Lines)
        {
            line.GlAccount = glAccounts[line.GlAccountId];
            if (line.CostCenterId.HasValue) line.CostCenter = costCenters[line.CostCenterId.Value];
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("MANUAL_JOURNAL_ENTRY", entry.Id.ToString(), entry.BranchId, "CREATED", "ACTIVITY",
                "created this manual journal entry", command.CreatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<ManualJournalEntryResponse>(notifyResult.Error!);

        return Result.Success(ManualJournalEntryMapper.ToResponse(entry));
    }
}
