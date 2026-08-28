namespace ZARI.Application.Features.Accounting.ManualJournalEntries.ApproveCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.ManualJournalEntries.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveManualJournalEntryCancellationCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<ManualJournalEntryResponse>>;
