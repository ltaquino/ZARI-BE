namespace ZARI.Application.Features.Accounting.ManualJournalEntries.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.ManualJournalEntries.Shared;
using ZARI.Domain.Common;

public sealed class GetAllManualJournalEntriesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllManualJournalEntriesQuery, Result<List<ManualJournalEntryResponse>>>
{
    public async Task<Result<List<ManualJournalEntryResponse>>> HandleAsync(GetAllManualJournalEntriesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("MANUAL_JOURNAL_ENTRIES", FormAction.View, cancellationToken))
            return Result.Failure<List<ManualJournalEntryResponse>>(Error.Forbidden("ManualJournalEntry.Forbidden", "You do not have permission to view manual journal entries."));

        var entries = await dbContext.ManualJournalEntries
            .Include(e => e.Lines).ThenInclude(l => l.GlAccount)
            .Include(e => e.Lines).ThenInclude(l => l.CostCenter)
            .OrderByDescending(e => e.EntryDate)
            .ToListAsync(cancellationToken);

        return Result.Success(entries.Select(ManualJournalEntryMapper.ToResponse).ToList());
    }
}
