namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// One installment row, materialized in full at LoanAccount creation time by
/// AmortizationScheduleGenerator (not lazily computed). PrincipalPaid/InterestPaid/PenaltyPaid/
/// Status stay at their zero/"DUE" defaults until LoanPayment starts allocating against them.
/// There is deliberately no PenaltyDue column — unlike PrincipalDue/InterestDue, a penalty can't be
/// precomputed at schedule-generation time because it depends on how late the member actually
/// pays; LoanPaymentAllocationEngine computes it live off LoanAccount.PenaltyRatePct/
/// GracePeriodDays at payment time (see LoanPenaltyCalculator), and only the cumulative amount
/// actually collected is persisted here, for audit/reporting.
/// </summary>
public sealed class LoanAmortizationScheduleLine : BaseEntity
{
    public Guid LoanAccountId { get; set; }
    public LoanAccount LoanAccount { get; set; } = default!;

    public int InstallmentNo { get; set; }
    public DateTimeOffset DueDate { get; set; }

    public decimal PrincipalDue { get; set; }
    public decimal InterestDue { get; set; }
    public decimal TotalDue { get; set; }
    public decimal OutstandingPrincipalAfter { get; set; }

    public decimal PrincipalPaid { get; set; }
    public decimal InterestPaid { get; set; }
    public decimal PenaltyPaid { get; set; }

    /// DUE | PARTIALLY_PAID | PAID | SUPERSEDED (the last set only on lines belonging to an account
    /// that's been rolled into a LoanRestructuring's new account — the line itself is untouched, just
    /// no longer payable).
    public string Status { get; set; } = default!;
}
