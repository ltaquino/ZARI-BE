namespace ZARI.Application.Features.Accounting.ManualJournalEntries.GetAllPaged;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.ManualJournalEntries.GetAll;
using ZARI.Application.Features.Accounting.ManualJournalEntries.Shared;
using ZARI.Domain.Common;

public sealed class GetAllManualJournalEntriesPagedQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllManualJournalEntriesPagedQuery, Result<PagedResult<ManualJournalEntryResponse>>>
{
    public async Task<Result<PagedResult<ManualJournalEntryResponse>>> HandleAsync(GetAllManualJournalEntriesPagedQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("MANUAL_JOURNAL_ENTRIES", FormAction.View, cancellationToken))
            return Result.Failure<PagedResult<ManualJournalEntryResponse>>(Error.Forbidden("ManualJournalEntry.Forbidden", "You do not have permission to view manual journal entrys."));

        var baseQuery = dbContext.ManualJournalEntries.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(x => x.EntryNo.Contains(query.Search) || x.Remarks.Contains(query.Search));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var entries = await baseQuery
            .OrderByDescending(e => e.EntryDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(e => e.Lines).ThenInclude(l => l.GlAccount)
            .Include(e => e.Lines).ThenInclude(l => l.CostCenter)
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<ManualJournalEntryResponse>(entries.Select(ManualJournalEntryMapper.ToResponse).ToList(), totalCount, query.Page, query.PageSize));
    }
}
