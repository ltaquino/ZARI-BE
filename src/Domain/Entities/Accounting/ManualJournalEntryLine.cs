namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// One debit-or-credit line of a manual journal entry — exactly one of DebitAmount/CreditAmount is
/// ever non-zero per line, enforced by the Create/Update validators, mirroring standard double-entry
/// line semantics. CostCenterId is optional and, unlike every other GL-posting module in this
/// system (which always passes null), is actually exposed on the form here — the first real
/// consumer of Cost Center as a transaction-level dimension rather than dead master data.
/// </summary>
public sealed class ManualJournalEntryLine : BaseEntity
{
    public Guid ManualJournalEntryId { get; set; }
    public ManualJournalEntry ManualJournalEntry { get; set; } = default!;
    public Guid GlAccountId { get; set; }
    public GlAccount GlAccount { get; set; } = default!;
    public Guid? CostCenterId { get; set; }
    public CostCenter? CostCenter { get; set; }
    public string? Memo { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
}
