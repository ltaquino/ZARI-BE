namespace ZARI.Application.Features.Accounting.ManualJournalEntries.Delete;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record DeleteManualJournalEntryCommand(Guid Id) : ICommand<Result>;
