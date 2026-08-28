namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// A user-authored GL journal — accruals, corrections, bank fees, depreciation, opening balances,
/// anything a subledger module (Inventory/Purchasing) can't produce on its own. Goes through the
/// same DRAFT/PENDING_APPROVAL/POSTED/PENDING_CANCELLATION/CANCELLED workflow as every other
/// document in this system; approving one posts a real GlJournal via PostGlJournalCommand
/// (SourceModule "ACCOUNTING", SourceReferenceTable "ManualJournalEntry"), and approving its
/// cancellation reverses that journal via ReverseGlJournalsCommand — the same generic engine every
/// other module already uses, just fed lines the user typed instead of ones a document derived.
/// </summary>
public sealed class ManualJournalEntry : AuditableEntity
{
    public string EntryNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;
    public DateTimeOffset EntryDate { get; set; }
    public string Status { get; set; } = default!;

    // Required — unlike a document-driven journal, there's no originating transaction to explain
    // itself, so the reason has to be typed by hand for the audit trail.
    public string Remarks { get; set; } = default!;

    public List<ManualJournalEntryLine> Lines { get; set; } = [];

    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
