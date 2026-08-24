namespace ZARI.Application.Features.Accounting.GlJournals.Shared;

using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Domain.Entities;

internal static class GlJournalMapper
{
    public static GlJournalResponse ToResponse(GlJournal journal) => new(
        journal.Id,
        journal.JournalNo,
        journal.BranchId,
        journal.JournalDate,
        journal.SourceModule,
        journal.SourceReferenceTable,
        journal.SourceReferenceId,
        journal.Description,
        journal.Status,
        journal.ReversalOfJournalId,
        journal.Lines.Select(l => new GlJournalLineResponse(l.Id, l.AccountId, l.CostCenterId, l.DebitAmount, l.CreditAmount, l.Memo)).ToList(),
        journal.CreatedAt);
}
