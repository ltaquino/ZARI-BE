namespace ZARI.Application.Features.Loan.Reports.CdaPortfolioQuality.Shared;

using ZARI.Domain.Entities;

/// <summary>
/// MC 02-04's own definition of "Loans Restructured": performing/current once there's "a good
/// tracking record of continuous payment ... using the following installments: 3 installments for
/// monthly; 4 installments for semi-monthly; 6 installments for weekly and 10 installments for
/// daily. It will be considered past due when it is not performing, i.e., there are no payments
/// received after restructuring." Applied here to a restructured account's own new schedule
/// (installment 1 onward), counting only a contiguous run of PAID lines from the start — a gap
/// means it hasn't "graduated" yet even if later installments were paid out of order.
/// </summary>
internal static class RestructuredLoanGraduation
{
    // "Daily" has no equivalent RepaymentFrequency in this codebase (only MONTHLY/SEMI_MONTHLY/
    // WEEKLY are implemented) — no threshold to map it to, so it's simply never reached.
    private static int RequiredConsecutivePayments(string repaymentFrequency) => repaymentFrequency switch
    {
        "MONTHLY" => 3,
        "SEMI_MONTHLY" => 4,
        "WEEKLY" => 6,
        _ => 3,
    };

    /// <summary>
    /// Returns true once the account has graduated back to CURRENT, false while still tracked as
    /// RESTRUCTURED or (if it has zero payments at all) PAST_DUE — see HasAnyPayment for that split.
    /// </summary>
    public static bool HasGraduated(LoanAccount account)
    {
        var required = RequiredConsecutivePayments(account.RepaymentFrequency);
        var consecutivePaid = account.ScheduleLines
            .OrderBy(l => l.InstallmentNo)
            .TakeWhile(l => l.Status == "PAID")
            .Count();
        return consecutivePaid >= required;
    }

    public static bool HasAnyPayment(LoanAccount account) =>
        account.ScheduleLines.Any(l => l.PrincipalPaid > 0 || l.InterestPaid > 0);
}
