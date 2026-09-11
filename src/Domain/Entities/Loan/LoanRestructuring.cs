namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Rolls an ACTIVE loan account's outstanding principal into a brand-new LoanAccount under
/// renegotiated terms — same "upstream document spawns a downstream one" shape as
/// LoanApplication -> LoanAccount, not an in-place edit (LoanAccount.Status already has a terminal
/// "RESTRUCTURED" value reserved for exactly this). DRAFT -> PENDING_APPROVAL -> POSTED like
/// LoanDisbursement, with the same two-tier cancellation once POSTED (a posted restructuring has
/// already spawned a real LoanAccount and moved the ledger balance across two accounts).
///
/// v1 policy (no existing precedent to lean on — genuinely new mechanics): any interest or penalty
/// already OWED — i.e. on installments whose DueDate has passed — must already be settled (paid
/// down to ~zero) before a restructuring can be created or approved (LoanRestructuringEligibility).
/// Future, not-yet-due installments' interest isn't money owed yet, so superseding them as part of
/// the roll-forward doesn't write anything off. This keeps the mechanics to a pure principal
/// roll-forward, with nothing silently written off and no interest-capitalization policy to invent.
/// Only PRINCIPAL carries into the new schedule. OldPrincipalBalance is snapshotted at Create time
/// for display, but Approve always re-derives it fresh off the OLD account's ledger running balance
/// (never trusts the possibly-stale Create-time snapshot).
/// </summary>
public sealed class LoanRestructuring : AuditableEntity
{
    public string RestructuringNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;

    public Guid OldLoanAccountId { get; set; }
    public LoanAccount OldLoanAccount { get; set; } = default!;

    // Populated only once POSTED.
    public Guid? NewLoanAccountId { get; set; }
    public LoanAccount? NewLoanAccount { get; set; }

    public DateTimeOffset RestructureDate { get; set; }
    public decimal OldPrincipalBalance { get; set; }

    public decimal NewAnnualInterestRatePct { get; set; }
    public int NewTermMonths { get; set; }
    public string NewRepaymentFrequency { get; set; } = default!;
    public int NewGracePeriodDays { get; set; }
    public decimal NewPenaltyRatePct { get; set; }
    public DateTimeOffset NewFirstDueDate { get; set; }

    public string Reason { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }

    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
