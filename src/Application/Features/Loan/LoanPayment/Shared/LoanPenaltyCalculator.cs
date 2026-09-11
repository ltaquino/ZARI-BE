namespace ZARI.Application.Features.Loan.LoanPayments.Shared;

using ZARI.Domain.Entities;

/// <summary>
/// v1 penalty policy (ZARI-FE/frs/loan/LoanModuleContext.md §6 flags the exact computation rule as
/// an open question the cooperative still needs to confirm): LoanProduct/LoanAccount's
/// PenaltyRatePct is treated as a monthly rate, pro-rated per day overdue, applied against an
/// installment's still-unpaid principal+interest once it's been overdue longer than
/// GracePeriodDays past its DueDate. No compounding — a straight daily accrual on the outstanding
/// installment amount, computed live (never stored ahead of time, since it depends on the actual
/// payment date). Flag this assumption to the cooperative before relying on it for real collection.
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
