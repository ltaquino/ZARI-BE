namespace ZARI.Application.Features.Loan.LoanPayments.Shared;

using ZARI.Domain.Entities;

/// <summary>
/// Penalty policy — DECIDED per ZARI-FE/frs/loan/LoanModuleContext.md §6 (previously an open
/// question, now confirmed as the standard formula to use): PenaltyRatePct is set per LoanProduct
/// ("loan account type") and snapshotted onto each LoanAccount at creation, exactly like the
/// interest rate/term/frequency it's alongside — no separate per-account override needed, the same
/// product-level knob every other loan term already uses. Treated as a monthly rate, pro-rated per
/// day overdue, applied against an installment's still-unpaid principal+interest once it's been
/// overdue longer than GracePeriodDays past its DueDate. No compounding — a straight daily accrual
/// on the outstanding installment amount, computed live (never stored ahead of time, since it
/// depends on the actual payment date).
/// </summary>
internal static class LoanPenaltyCalculator
{
    public static decimal ComputeOwed(LoanAmortizationScheduleLine line, DateTimeOffset asOfDate, decimal penaltyRatePct, int gracePeriodDays)
    {
        var penaltyStartsOn = line.DueDate.AddDays(gracePeriodDays);
        if (asOfDate.Date <= penaltyStartsOn.Date)
            return 0m;

        var daysLate = (asOfDate.Date - penaltyStartsOn.Date).Days;
        var outstandingInstallment = (line.PrincipalDue - line.PrincipalPaid) + (line.InterestDue - line.InterestPaid);
        if (outstandingInstallment <= 0)
            return 0m;

        var accrued = outstandingInstallment * (penaltyRatePct / 100m) * daysLate / 30m;
        var owed = Math.Round(accrued, 2) - line.PenaltyPaid;
        return owed > 0 ? owed : 0m;
    }
}
