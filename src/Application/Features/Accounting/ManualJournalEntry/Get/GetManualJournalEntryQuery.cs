namespace ZARI.Application.Features.Accounting.ManualJournalEntries.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.ManualJournalEntries.GetAll;
using ZARI.Domain.Common;

public sealed record GetManualJournalEntryQuery(Guid Id) : IQuery<Result<ManualJournalEntryResponse>>;
