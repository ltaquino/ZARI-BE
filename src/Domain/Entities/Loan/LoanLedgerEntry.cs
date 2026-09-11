namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Immutable append-only running-principal-balance log — the StockLedger idiom applied to loans
/// (ZARI-FE/frs/loan/LoanModuleContext.md §4.7). Rows are only ever appended (by
/// PostLoanLedgerEntryCommandHandler, called from LoanDisbursement's Approve/ApproveCancellation
/// and, later, LoanPayment/LoanRestructuring/LoanWriteOff); nothing ever updates or deletes one.
/// Unlike StockLedger, there's no separate balance-cache table — a LoanAccount's ledger is
/// naturally scoped to one account, so the previous row's RunningPrincipalBalance (read under a
/// FOR UPDATE lock on the LoanAccount row itself) is the running total.
/// </summary>
public sealed class LoanLedgerEntry : AuditableEntity
{
    public Guid LoanAccountId { get; set; }
    public LoanAccount LoanAccount { get; set; } = default!;

    /// Strictly increasing per LoanAccount — the reliable ordering key (timestamps alone can tie).
    public int SequenceNo { get; set; }

    public DateTimeOffset EntryDate { get; set; }

    /// DISBURSEMENT | PAYMENT | RESTRUCTURING | WRITE_OFF — only DISBURSEMENT is produced so far.
    public string TransactionType { get; set; } = default!;

    public string ReferenceTable { get; set; } = default!;
    public string ReferenceId { get; set; } = default!;

    public decimal PrincipalIn { get; set; }
    public decimal PrincipalOut { get; set; }
    public decimal RunningPrincipalBalance { get; set; }

    /// Flags a row posted purely to undo a cancelled/reversed document — same idiom as StockLedger.
    public bool IsReversal { get; set; }

    public string? Description { get; set; }
    public DateTimeOffset PostedAt { get; set; }
}
