namespace ZARI.Application.Features.Accounting.ManualJournalEntries.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.ManualJournalEntries.GetAll;
using ZARI.Domain.Common;

public sealed record ManualJournalEntryLineInput(
    Guid GlAccountId,
    Guid? CostCenterId,
    string? Memo,
    decimal DebitAmount,
    decimal CreditAmount);

public sealed record CreateManualJournalEntryCommand(
    string BranchId,
    DateTimeOffset EntryDate,
    string Remarks,
    string? CreatedBy,
    List<ManualJournalEntryLineInput> Lines) : ICommand<Result<ManualJournalEntryResponse>>;
