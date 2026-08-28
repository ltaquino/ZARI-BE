namespace ZARI.Application.Features.Accounting.ManualJournalEntries.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.ManualJournalEntries.Create;
using ZARI.Application.Features.Accounting.ManualJournalEntries.GetAll;
using ZARI.Domain.Common;

public sealed record UpdateManualJournalEntryCommand(
    Guid Id,
    DateTimeOffset EntryDate,
    string Remarks,
    string? UpdatedBy,
    List<ManualJournalEntryLineInput> Lines) : ICommand<Result<ManualJournalEntryResponse>>;
