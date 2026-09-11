namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Collects a member's payment against an ACTIVE LoanAccount and quick-posts immediately at
/// Create — there's no DRAFT/Submit/Approve step (ZARI-FE/frs/loan/LoanModuleContext.md §4.8:
/// "money received, immediately real," the same convention CustomerPayment's quick-post path
/// uses). Allocates oldest-installment-first across LoanAmortizationScheduleLines, Penalty ->
/// Interest -> Principal per installment (standard collection priority) up to each installment's
/// outstanding amount; the payment total must not exceed the account's total outstanding
/// (principal + interest + live-computed penalty) — no overpayment/prepayment handling in v1 (§6,
/// still an open question). Because it's always already POSTED the moment it exists, cancelling
/// it is a single-tier RequestCancellation/ApproveCancellation/RejectCancellation flow only
/// (mirrors LoanDisbursement's POSTED-tier cancellation, minus the pre-post Cancel action that
/// never applies here) — approving a cancellation reverses the GL journal, reverses the
/// LoanLedgerEntry, and unwinds every touched schedule line's
/// PrincipalPaid/InterestPaid/PenaltyPaid/Status back to what they were before.
/// </summary>
public sealed class LoanPayment : AuditableEntity
{
    public string PaymentNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;

    public Guid LoanAccountId { get; set; }
    public LoanAccount LoanAccount { get; set; } = default!;

    public DateTimeOffset PaymentDate { get; set; }
    public decimal Amount { get; set; }

    public Guid PaymentMethodId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = default!;
    public string? ReferenceNo { get; set; }

    public Guid? CostCenterId { get; set; }
    public CostCenter? CostCenter { get; set; }

    /// POSTED -> PENDING_CANCELLATION -> CANCELLED, or POSTED -> PENDING_CANCELLATION -> POSTED
    /// (rejected). Never DRAFT/PENDING_APPROVAL — see class doc comment.
    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }

    public List<LoanPaymentAllocation> Allocations { get; set; } = [];

    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
