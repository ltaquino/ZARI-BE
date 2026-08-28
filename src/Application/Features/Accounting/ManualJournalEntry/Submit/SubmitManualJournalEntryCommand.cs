namespace ZARI.Application.Features.Accounting.ManualJournalEntries.Submit;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.ManualJournalEntries.GetAll;
using ZARI.Domain.Common;

public sealed record SubmitManualJournalEntryCommand(Guid Id, string RequestedBy) : ICommand<Result<ManualJournalEntryResponse>>;
