namespace ZARI.Application.Features.Loan.LoanPayments.Shared;

using ZARI.Domain.Entities;

internal sealed record LoanPaymentLineAllocation(LoanAmortizationScheduleLine Line, decimal PrincipalApplied, decimal InterestApplied, decimal PenaltyApplied);

internal sealed record LoanPaymentAllocationResult(List<LoanPaymentLineAllocation> Lines, decimal PrincipalTotal, decimal InterestTotal, decimal PenaltyTotal, decimal TotalOutstanding);

/// <summary>
/// Oldest-installment-first, Penalty -> Interest -> Principal per installment (standard collection
/// priority) — ZARI-FE/frs/loan/LoanModuleContext.md §4.8. Mutates each touched line's
/// PrincipalPaid/InterestPaid/PenaltyPaid/Status directly (the caller persists them); always
/// computes and returns TotalOutstanding regardless of the amount tendered, so the caller can
/// reject an amount that exceeds it (no overpayment/prepayment support in v1 — still an open
/// question per §6) before ever saving anything.
/// </summary>
internal static class LoanPaymentAllocationEngine
{
    public static LoanPaymentAllocationResult Allocate(
        IEnumerable<LoanAmortizationScheduleLine> scheduleLines, DateTimeOffset paymentDate, decimal amount, decimal penaltyRatePct, int gracePeriodDays)
    {
        var orderedLines = scheduleLines.Where(l => l.Status != "PAID").OrderBy(l => l.InstallmentNo).ToList();

        var totalOutstanding = 0m;
        foreach (var line in orderedLines)
        {
            var penaltyOwed = LoanPenaltyCalculator.ComputeOwed(line, paymentDate, penaltyRatePct, gracePeriodDays);
            var interestOwed = line.InterestDue - line.InterestPaid;
            var principalOwed = line.PrincipalDue - line.PrincipalPaid;
            totalOutstanding += penaltyOwed + interestOwed + principalOwed;
        }
        totalOutstanding = Math.Round(totalOutstanding, 2);

        var remaining = amount;
        var lines = new List<LoanPaymentLineAllocation>();
        decimal principalTotal = 0, interestTotal = 0, penaltyTotal = 0;

        foreach (var line in orderedLines)
        {
            if (remaining <= 0) break;

            var penaltyOwed = LoanPenaltyCalculator.ComputeOwed(line, paymentDate, penaltyRatePct, gracePeriodDays);
            var interestOwed = line.InterestDue - line.InterestPaid;
            var principalOwed = line.PrincipalDue - line.PrincipalPaid;

            var penaltyApplied = Math.Min(remaining, penaltyOwed);
            remaining -= penaltyApplied;
            var interestApplied = Math.Min(remaining, interestOwed);
            remaining -= interestApplied;
            var principalApplied = Math.Min(remaining, principalOwed);
            remaining -= principalApplied;

            if (penaltyApplied <= 0 && interestApplied <= 0 && principalApplied <= 0)
                continue;

            line.PenaltyPaid += penaltyApplied;
            line.InterestPaid += interestApplied;
            line.PrincipalPaid += principalApplied;
            line.Status = line.PrincipalPaid >= line.PrincipalDue && line.InterestPaid >= line.InterestDue
                ? "PAID"
                : "PARTIALLY_PAID";

            lines.Add(new LoanPaymentLineAllocation(line, principalApplied, interestApplied, penaltyApplied));
            principalTotal += principalApplied;
            interestTotal += interestApplied;
            penaltyTotal += penaltyApplied;
        }

        return new LoanPaymentAllocationResult(lines, principalTotal, interestTotal, penaltyTotal, totalOutstanding);
    }
}
