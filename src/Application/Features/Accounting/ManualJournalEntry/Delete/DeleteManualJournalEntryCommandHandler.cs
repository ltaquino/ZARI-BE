namespace ZARI.Application.Features.Accounting.ManualJournalEntries.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteManualJournalEntryCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteManualJournalEntryCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteManualJournalEntryCommand command, CancellationToken cancellationToken = default)
    {
        var entry = await dbContext.ManualJournalEntries.FindAsync([command.Id], cancellationToken);
        if (entry is null)
            return Result.Failure(Error.NotFound("ManualJournalEntry.NotFound", $"Manual journal entry with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("MANUAL_JOURNAL_ENTRIES", FormAction.Delete, entry.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("ManualJournalEntry.Forbidden", "You do not have permission to delete manual journal entries for this branch."));

        if (entry.Status != "DRAFT")
            return Result.Failure(Error.Validation("ManualJournalEntry.NotDraft", "Only draft manual journal entries can be deleted — cancel it instead."));

        dbContext.ManualJournalEntries.Remove(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
