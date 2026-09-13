namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// The GL-posting document that actually releases loan proceeds — DRAFT -> PENDING_APPROVAL ->
/// POSTED (like GoodsReceiptPo), with the same two-tier cancellation (single-tier Cancel pre-post,
/// RequestCancellation/ApproveCancellation/RejectCancellation once POSTED, since a posted
/// disbursement has already moved real money and posted a real GL journal). Approving it posts
/// Dr Loans Receivable / Cr the funding PaymentMethod's GL account and flips the referenced
/// LoanAccount PENDING_DISBURSEMENT -> ACTIVE. v1 supports exactly one disbursement per
/// LoanAccount (Amount must equal the account's full PrincipalAmount) — staggered/partial release
/// is explicitly deferred (ZARI-FE/frs/loan/LoanModuleContext.md §4.6).
/// </summary>
public sealed class LoanDisbursement : AuditableEntity
{
    public string DisbursementNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;

    public Guid LoanAccountId { get; set; }
    public LoanAccount LoanAccount { get; set; } = default!;

    public DateTimeOffset DisbursementDate { get; set; }
    public decimal Amount { get; set; }

    public Guid PaymentMethodId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = default!;
    public string? ReferenceNo { get; set; }

    public Guid? CostCenterId { get; set; }
    public CostCenter? CostCenter { get; set; }

    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }

    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
