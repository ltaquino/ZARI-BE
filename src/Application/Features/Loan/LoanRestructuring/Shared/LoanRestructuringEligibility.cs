namespace ZARI.Application.Features.Loan.LoanRestructurings.Shared;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Features.Loan.LoanPayments.Shared;
using ZARI.Domain.Entities;

/// <summary>
/// Shared by Create (a friendly, non-authoritative check) and Approve (the authoritative re-check —
/// arrears could have accrued, or been paid down, in the gap between the two). See
/// LoanRestructuring's own class doc comment for why arrears must be zero: v1 only rolls forward
/// principal, so nothing can be silently written off.
/// </summary>
internal static class LoanRestructuringEligibility
{
    /// <summary>
    /// Only installments that have actually come due (DueDate &lt;= asOfDate) count as "arrears" —
    /// a not-yet-due future installment's interest isn't money owed yet, so superseding it as part
    /// of the roll-forward doesn't write anything off. Restricting the check to already-due lines
    /// is what makes restructuring usable in practice: requiring every future installment's interest
    /// to be prepaid first (as an earlier draft of this check did) would make almost no real loan
    /// eligible.
    /// </summary>
    public static decimal ComputeUnpaidInterestAndPenalty(LoanAccount account, DateTimeOffset asOfDate) =>
        account.ScheduleLines
            .Where(l => l.Status != "PAID" && l.Status != "SUPERSEDED" && l.DueDate <= asOfDate)
            .Sum(l => (l.InterestDue - l.InterestPaid) + LoanPenaltyCalculator.ComputeOwed(l, asOfDate, account.PenaltyRatePct, account.GracePeriodDays));

    public static async Task<decimal> GetCurrentPrincipalBalanceAsync(IAppDbContext dbContext, Guid loanAccountId, CancellationToken cancellationToken) =>
        await dbContext.LoanLedgerEntries
            .Where(l => l.LoanAccountId == loanAccountId)
            .OrderByDescending(l => l.SequenceNo)
            .Select(l => (decimal?)l.RunningPrincipalBalance)
            .FirstOrDefaultAsync(cancellationToken) ?? 0m;
}
