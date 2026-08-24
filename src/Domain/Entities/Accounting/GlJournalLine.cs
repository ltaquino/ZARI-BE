namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// A single debit/credit line of a GlJournal. No independent lifecycle of its own — created,
/// read, and (via a reversal) mirrored only ever together with its parent journal — so this is a
/// plain BaseEntity rather than AuditableEntity; the journal header carries the audit trail.
/// </summary>
public sealed class GlJournalLine : BaseEntity
{
    public Guid GlJournalId { get; set; }
    public GlJournal GlJournal { get; set; } = default!;

    public Guid AccountId { get; set; }
    public GlAccount Account { get; set; } = default!;

    public Guid? CostCenterId { get; set; }
    public CostCenter? CostCenter { get; set; }

    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string? Memo { get; set; }
}
