namespace ZARI.Application.Features.Accounting.ManualJournalEntries.Reject;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.ManualJournalEntries.GetAll;
using ZARI.Domain.Common;

public sealed record RejectManualJournalEntryCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<ManualJournalEntryResponse>>;
