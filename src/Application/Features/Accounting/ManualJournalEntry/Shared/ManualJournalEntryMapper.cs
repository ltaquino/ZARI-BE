namespace ZARI.Application.Features.Accounting.ManualJournalEntries.Shared;

using ZARI.Application.Features.Accounting.ManualJournalEntries.GetAll;
using ZARI.Domain.Entities;

internal static class ManualJournalEntryMapper
{
    public static ManualJournalEntryResponse ToResponse(ManualJournalEntry entry) => new(
        entry.Id,
        entry.EntryNo,
        entry.BranchId,
        entry.EntryDate,
        entry.Status,
        entry.Remarks,
        entry.Lines.Select(ToLineResponse).ToList(),
        entry.CancelledBy,
        entry.CancelledAt,
        entry.CancelReason,
        entry.CreatedAt,
        entry.CreatedBy);

    private static ManualJournalEntryLineResponse ToLineResponse(ManualJournalEntryLine line) => new(
        line.Id,
        line.GlAccountId,
        line.GlAccount.Code,
        line.GlAccount.Name,
        line.CostCenterId,
        line.CostCenter?.Code,
        line.CostCenter?.Name,
        line.Memo,
        line.DebitAmount,
        line.CreditAmount);
}
