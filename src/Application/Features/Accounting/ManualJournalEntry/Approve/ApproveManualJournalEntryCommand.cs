namespace ZARI.Application.Features.Accounting.ManualJournalEntries.Approve;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.ManualJournalEntries.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveManualJournalEntryCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<ManualJournalEntryResponse>>;
