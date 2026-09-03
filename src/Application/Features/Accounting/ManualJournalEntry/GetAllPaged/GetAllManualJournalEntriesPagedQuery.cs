namespace ZARI.Application.Features.Accounting.ManualJournalEntries.GetAllPaged;

using ZARI.Application.Features.Accounting.ManualJournalEntries.GetAll;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllManualJournalEntriesPagedQuery(int Page = 1, int PageSize = 20, string? Search = null) : IQuery<Result<PagedResult<ManualJournalEntryResponse>>>;
