namespace ZARI.Application.Features.Accounting.ManualJournalEntries.Cancel;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.ManualJournalEntries.GetAll;
using ZARI.Domain.Common;

public sealed record CancelManualJournalEntryCommand(Guid Id, string CancelledBy, string Reason) : ICommand<Result<ManualJournalEntryResponse>>;
