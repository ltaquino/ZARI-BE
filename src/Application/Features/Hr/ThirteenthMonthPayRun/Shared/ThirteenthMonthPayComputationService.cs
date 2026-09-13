namespace ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Shared;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// PD 851: 13th month pay = 1/12 of the total basic salary actually earned within the calendar
/// year. Rather than re-deriving "basic salary earned" from Employee.BasicSalary and a hire/
/// separation date (which would have to re-implement proration, unpaid-leave, and absence-deduction
/// logic PayrollComputationService already owns), this sums PayrollLine.BasicPay across every
/// PayrollRun that is (a) POSTED and (b) has a PayPeriodStart falling within the requested Year —
/// i.e., whatever was actually paid, which is correct regardless of pay cadence, mid-year hires, or
/// separations, since a PayrollLine only exists for pay periods the employee was actually eligible
/// for (see ComputePayrollRunCommandHandler's eligibility filter).
///
/// Sample hand-check: an employee with three POSTED semi-monthly PayrollLines this year of BasicPay
/// 10000.00, 10000.00, and 9500.00 (a half-period on separation) sums to 29500.00; divided by 12 =
/// 2458.33 (rounded to 2 decimals) — asserted byte-for-byte in
/// ThirteenthMonthPayComputationServiceTests.
///
/// v1 explicitly does NOT withhold tax on the resulting amount (real BIR rule: exempt up to
/// ₱90,000/year, taxable above) — a known simplification, not an oversight; see
/// ThirteenthMonthPayRun's own header comment.
/// </summary>
internal static class ThirteenthMonthPayComputationService
{
    public static async Task<(decimal TotalBasicPay, decimal ThirteenthMonthPay)> ComputeForEmployeeAsync(
        IAppDbContext dbContext, Guid employeeId, int year, CancellationToken cancellationToken)
    {
        var totalBasicPay = await dbContext.PayrollLines
            .Where(l => l.EmployeeId == employeeId
                && l.PayrollRun.Status == "POSTED"
                && l.PayrollRun.PayPeriodStart.Year == year)
            .SumAsync(l => l.BasicPay, cancellationToken);

        var thirteenthMonthPay = Math.Round(totalBasicPay / 12m, 2, MidpointRounding.AwayFromZero);
        return (totalBasicPay, thirteenthMonthPay);
    }
}
