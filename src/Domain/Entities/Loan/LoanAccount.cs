namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// A member's actual loan — created by referencing an APPROVED LoanApplication (same relationship
/// shape as PurchaseOrder->PurchaseRequest: the application's own status is left untouched, and a
/// LoanAccount can also be created without one for migrated/legacy loans). Interest rate, term and
/// repayment frequency are snapshotted from LoanProduct at creation time and never read live off
/// the product again, so a later product change can't corrupt a schedule already amortizing
/// (ZARI-FE/frs/loan/LoanModuleContext.md §4.4). Status: PENDING_DISBURSEMENT -> ACTIVE ->
/// FULLY_PAID | RESTRUCTURED | WRITTEN_OFF, or PENDING_DISBURSEMENT -> CANCELLED before
/// disbursement. "Past due" is a derived flag off the schedule lines, never a stored status.
/// </summary>
public sealed class LoanAccount : AuditableEntity
{
    public string LoanAcctNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;
    public Guid LoanProductId { get; set; }
    public LoanProduct LoanProduct { get; set; } = default!;
    public Guid? LoanApplicationId { get; set; }
    public LoanApplication? LoanApplication { get; set; }

    public decimal PrincipalAmount { get; set; }

    // Snapshot of LoanProduct terms at creation time — see class doc comment.
    public decimal AnnualInterestRatePct { get; set; }
    public int TermMonths { get; set; }
    public string RepaymentFrequency { get; set; } = default!;
    public int GracePeriodDays { get; set; }
    public decimal PenaltyRatePct { get; set; }

    public DateTimeOffset GrantDate { get; set; }
    public DateTimeOffset FirstDueDate { get; set; }

    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }

    // Per-account GL overrides, falling back to LoanProduct's own accounts when null — same
    // override-with-fallback idiom as Customer.ArAccountId.
    public Guid? LoanReceivableAccountId { get; set; }
    public GlAccount? LoanReceivableAccount { get; set; }
    public Guid? InterestIncomeAccountId { get; set; }
    public GlAccount? InterestIncomeAccount { get; set; }
    public Guid? PenaltyIncomeAccountId { get; set; }
    public GlAccount? PenaltyIncomeAccount { get; set; }

    public List<LoanAmortizationScheduleLine> ScheduleLines { get; set; } = [];

    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }

    // CISA (RA 9510 / CDA MC 2019-01) negative-credit-information flag — "pending litigation
    // affecting creditworthiness" has no other home in this module (not a loan Status value, since
    // a dispute doesn't change what's owed or the workflow state) and can be raised or cleared at
    // any point in the account's life, unlike every other mutation on this entity which is
    // status-gated — so it's set via its own dedicated command
    // (Features/Loan/LoanAccount/SetDisputeStatus), not folded into Update.
    public bool IsDisputed { get; set; }
    public string? DisputeNotes { get; set; }
}
