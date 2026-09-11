namespace ZARI.Application.Features.Loan.LoanAccounts.Shared;

/// <summary>
/// Diminishing-balance (equal-installment / standard amortizing) schedule generator — the one
/// genuinely new calculation in the Loan module (ZARI-FE/frs/loan/LoanModuleContext.md §3.5, §4.5).
/// Hand-verified against a ₱10,000 principal / 12% annual / 12 monthly installments example before
/// anything downstream was built on it: level installment ≈ ₱888.49/mo, total interest ≈ ₱661.86,
/// and the balance lands exactly at ₱0.00 — the last installment absorbs whatever rounding
/// residual accrued across the prior periods so the schedule always fully amortizes to zero.
/// </summary>
internal static class AmortizationScheduleGenerator
{
    public sealed record ScheduleLineDraft(int InstallmentNo, DateTimeOffset DueDate, decimal PrincipalDue, decimal InterestDue, decimal OutstandingPrincipalAfter);

    public static List<ScheduleLineDraft> Generate(decimal principal, decimal annualRatePct, int termMonths, string repaymentFrequency, DateTimeOffset firstDueDate)
    {
        var periodsPerYear = repaymentFrequency switch
        {
            "WEEKLY" => 52,
            "SEMI_MONTHLY" => 24,
            _ => 12 // MONTHLY (default/fallback)
        };

        var installmentCount = Math.Max(1, (int)Math.Round(termMonths * periodsPerYear / 12m, MidpointRounding.AwayFromZero));
        var ratePerPeriod = annualRatePct / 100m / periodsPerYear;

        var levelInstallment = ratePerPeriod == 0m
            ? principal / installmentCount
            : principal * (ratePerPeriod * Pow(1 + ratePerPeriod, installmentCount)) / (Pow(1 + ratePerPeriod, installmentCount) - 1);

        var lines = new List<ScheduleLineDraft>(installmentCount);
        var balance = principal;
        var dueDate = firstDueDate;

        for (var i = 1; i <= installmentCount; i++)
        {
            var interestDue = Math.Round(balance * ratePerPeriod, 2, MidpointRounding.AwayFromZero);
            var principalDue = i == installmentCount
                ? Math.Round(balance, 2, MidpointRounding.AwayFromZero)
                : Math.Round(levelInstallment, 2, MidpointRounding.AwayFromZero) - interestDue;

            balance = Math.Round(balance - principalDue, 2, MidpointRounding.AwayFromZero);
            lines.Add(new ScheduleLineDraft(i, dueDate, principalDue, interestDue, balance));

            dueDate = repaymentFrequency switch
            {
                "WEEKLY" => dueDate.AddDays(7),
                "SEMI_MONTHLY" => dueDate.AddDays(15),
                _ => dueDate.AddMonths(1)
            };
        }

        return lines;
    }

    private static decimal Pow(decimal baseValue, int exponent)
    {
        var result = 1m;
        for (var i = 0; i < exponent; i++)
            result *= baseValue;
        return result;
    }
}
