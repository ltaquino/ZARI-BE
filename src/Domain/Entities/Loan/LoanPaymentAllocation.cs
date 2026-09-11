namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// One row per LoanAmortizationScheduleLine a LoanPayment touched — records exactly how much of
/// that payment applied as principal/interest/penalty against that installment, so a later
/// cancellation can unwind precisely these amounts from the schedule line rather than
/// re-deriving them.
/// </summary>
public sealed class LoanPaymentAllocation : BaseEntity
{
    public Guid LoanPaymentId { get; set; }
    public LoanPayment LoanPayment { get; set; } = default!;

    public Guid LoanAmortizationScheduleLineId { get; set; }
    public LoanAmortizationScheduleLine LoanAmortizationScheduleLine { get; set; } = default!;

    public decimal PrincipalApplied { get; set; }
    public decimal InterestApplied { get; set; }
    public decimal PenaltyApplied { get; set; }
}
