namespace ZARI.Application.Features.Accounting.ManualJournalEntries.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllManualJournalEntriesQuery : IQuery<Result<List<ManualJournalEntryResponse>>>;

public sealed record ManualJournalEntryLineResponse(
    Guid Id,
    Guid GlAccountId,
    string GlAccountCode,
    string GlAccountName,
    Guid? CostCenterId,
    string? CostCenterCode,
    string? CostCenterName,
    string? Memo,
    decimal DebitAmount,
    decimal CreditAmount);

public sealed record ManualJournalEntryResponse(
    Guid Id,
    string EntryNo,
    string BranchId,
    DateTimeOffset EntryDate,
    string Status,
    string Remarks,
    List<ManualJournalEntryLineResponse> Lines,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
