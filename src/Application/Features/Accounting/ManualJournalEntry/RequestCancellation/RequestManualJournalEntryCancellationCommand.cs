namespace ZARI.Application.Features.Accounting.ManualJournalEntries.RequestCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.ManualJournalEntries.GetAll;
using ZARI.Domain.Common;

public sealed record RequestManualJournalEntryCancellationCommand(Guid Id, string RequestedBy, string Reason) : ICommand<Result<ManualJournalEntryResponse>>;
