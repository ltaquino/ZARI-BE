namespace ZARI.Application.Features.Hr.PayrollRuns.Shared;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Features.Hr.DailyTimeRecords.Shared;
using ZARI.Domain.Entities;

/// <summary>
/// The one genuinely new calculation in the HR module (HrModuleContext.md §3.13/§3.14/§4 step 9) —
/// hand-verified against a manually-computed sample before anything downstream (ComputePayrollRun,
/// the live smoke test) was built on it, same discipline as Loan's own AmortizationScheduleGenerator.
///
/// Sample: Employee.BasicSalary = 20000 (monthly), no allowances/deductions/OT this period.
///   BasicPay = 20000 / 2 = 10000.00 (semi-monthly split — see the cadence note below)
///   SSS bracket [0, 25000): EmployeeShare 500/mo, EmployerShare 1000/mo -> this line: 250.00 / 500.00
///   PhilHealth [0, 100000): 2% / 2% -> monthly 400/400 -> this line: 200.00 / 200.00
///   Pag-IBIG bracket [0, 100000): EmployeeShare 200/mo, EmployerShare 400/mo (fixed peso, same
///     shape as SSS, not a percentage) -> this line: 100.00 / 200.00
///   GrossPay = 10000 + 0 + 0 = 10000.00
///   TaxableIncome = 10000 - 250 - 200 - 100 = 9450.00 -> below the sample bracket's SalaryFrom -> WithholdingTax = 0
///   NetPay = 10000 - 250 - 200 - 100 - 0 - 0 = 9450.00
/// A second sample with 2 hours of approved ordinary OT (BasicSalary 17600 -> hourly rate 100.00
/// exactly, requested 2h, DTR shows 3h actually worked past schedule, OvertimeRateRule 1.25x):
/// payable hours = min(2, 3) = 2 -> OvertimePay = 2 * 100.00 * 1.25 = 250.00.
/// See PayrollComputationServiceTests for both worked all the way through.
///
/// Absence/late/undertime handling (confirmed with the cooperative 2026-09-13): consume leave
/// balance first, then deduct pay per hour for whatever isn't covered — see ComputeAbsenceAsync.
///
/// **V1 assumptions, deliberately not invented further than this — flag before relying on real
/// output** (HrModuleContext.md §6): pay cadence is semi-monthly (BasicPay = BasicSalary / 2,
/// regardless of the run's actual PayPeriodStart/End, confirmed with the cooperative); a new hire
/// or separated employee is either fully included or fully excluded for the run, no partial-period
/// proration; government
/// contribution/withholding brackets are looked up against the employee's full monthly BasicSalary
/// (not the semi-monthly BasicPay) and the resulting monthly contribution is halved for this run —
/// SSS/PhilHealth/Pag-IBIG are commonly withheld only once a month in real practice, this is a
/// simplification; the OT hourly-rate divisor is 22 working days x 8 hours = 176 hours/month, a
/// common PH convention but not the only one; EmployeeAllowance/EmployeeDeduction amounts are
/// applied in full for every run they're effective for (HR is expected to enter the per-run figure
/// directly, not a monthly one); WithholdingTaxBracket.RateOverExcess is a fraction (0.15 = 15%),
/// not a raw percentage number, unlike GovernmentContributionRate's *Pct fields. None of these
/// override the still-fully-open policy questions (§6): exact leave entitlements, OT multipliers,
/// and the real current SSS/PhilHealth/Pag-IBIG/BIR bracket values are not seeded and must come from
/// the cooperative's actual HR/payroll officer before this generator's output is trusted for a real
/// payroll.
/// </summary>
public static class PayrollComputationService
{
    private const decimal WorkingDaysPerMonth = 22m;
    private const decimal HoursPerDay = 8m;

    public sealed record PayrollLineDraft(
        Guid EmployeeId,
        decimal BasicPay,
        decimal OvertimePay,
        decimal AllowanceTotal,
        decimal LeaveDaysConsumed,
        Guid? LeaveTypeConsumedId,
        decimal AbsenceDeduction,
        decimal GrossPay,
        decimal SssEmployeeShare,
        decimal SssEmployerShare,
        decimal PhilHealthEmployeeShare,
        decimal PhilHealthEmployerShare,
        decimal PagIbigEmployeeShare,
        decimal PagIbigEmployerShare,
        decimal WithholdingTax,
        decimal DeductionTotal,
        decimal NetPay,
        decimal ReceivableRecoveryDeduction = 0);

    public static async Task<PayrollLineDraft> ComputeForEmployeeAsync(IAppDbContext dbContext, Employee employee, PayrollRun run, CancellationToken cancellationToken)
    {
        var basicPay = Round(employee.BasicSalary / 2m);
        var overtimePay = await ComputeOvertimePayAsync(dbContext, employee, run, cancellationToken);
        var (leaveDaysConsumed, leaveTypeConsumedId, absenceDeduction) = await ComputeAbsenceAsync(dbContext, employee, run, cancellationToken);

        var allowanceTotal = await dbContext.EmployeeAllowances
            .Where(a => a.EmployeeId == employee.Id && a.Status == "active"
                && a.EffectiveDate <= run.PayPeriodEnd && (a.EndDate == null || a.EndDate >= run.PayPeriodStart))
            .SumAsync(a => (decimal?)a.Amount, cancellationToken) ?? 0m;

        var grossPay = basicPay - absenceDeduction + overtimePay + allowanceTotal;

        var (sssEe, sssEr) = await ComputeSssAsync(dbContext, employee, run, cancellationToken);
        var (phEe, phEr) = await ComputeGovernmentPctAsync(dbContext, employee, run, "PHILHEALTH", cancellationToken);
        var (pgEe, pgEr) = await ComputePagIbigAsync(dbContext, employee, run, cancellationToken);

        var taxableIncome = grossPay - sssEe - phEe - pgEe;
        var withholdingTax = await ComputeWithholdingTaxAsync(dbContext, taxableIncome, run, cancellationToken);

        var deductions = await dbContext.EmployeeDeductions
            .Where(d => d.EmployeeId == employee.Id && d.Status == "active"
                && d.EffectiveDate <= run.PayPeriodEnd && (d.EndDate == null || d.EndDate >= run.PayPeriodStart))
            .Select(d => new { d.Code, d.Amount })
            .ToListAsync(cancellationToken);
        var deductionTotal = deductions.Sum(d => d.Amount);
        var receivableRecovery = deductions.Where(d => EmployeeDeduction.ReceivableRecoveryCodes.Contains(d.Code)).Sum(d => d.Amount);

        var netPay = grossPay - sssEe - phEe - pgEe - withholdingTax - deductionTotal;

        return new PayrollLineDraft(employee.Id, basicPay, overtimePay, allowanceTotal, leaveDaysConsumed, leaveTypeConsumedId, absenceDeduction, grossPay,
            sssEe, sssEr, phEe, phEr, pgEe, pgEr, withholdingTax, deductionTotal, netPay, receivableRecovery);
    }

    /// <summary>
    /// Unplanned absence/late/undertime, plus any explicitly-unpaid day of an already-approved
    /// leave, is first covered by an automatic draw against whichever active, paid LeaveType
    /// currently holds the employee's largest balance — a defensible default that needs no
    /// per-employee "primary leave type" configuration. Only the shortfall left after that draw
    /// (insufficient/no balance) becomes a pay deduction, computed per hour at the same hourly rate
    /// used for OT. A day already covered by an APPROVED LeaveRequest is skipped entirely if its
    /// LeaveType.IsPaid — the LeaveRequest's own Approve step already posted that USAGE row, this
    /// method must never double-count it. This is a preview only: it never posts to the
    /// LeaveBalanceEntry ledger itself (that would double-post on every Compute re-run while still
    /// DRAFT) — the real USAGE row is posted once, by ApprovePayrollRunCommandHandler.
    /// </summary>
    private static async Task<(decimal LeaveDaysConsumed, Guid? LeaveTypeConsumedId, decimal AbsenceDeduction)> ComputeAbsenceAsync(
        IAppDbContext dbContext, Employee employee, PayrollRun run, CancellationToken cancellationToken)
    {
        var approvedLeaves = await dbContext.LeaveRequests
            .Where(r => r.EmployeeId == employee.Id && r.Status == "APPROVED" && r.DateFrom <= run.PayPeriodEnd && r.DateTo >= run.PayPeriodStart)
            .Select(r => new { r.DateFrom, r.DateTo, r.LeaveTypeId })
            .ToListAsync(cancellationToken);

        var leaveTypeIsPaidCache = new Dictionary<Guid, bool>();
        async Task<bool> IsPaidAsync(Guid leaveTypeId)
        {
            if (leaveTypeIsPaidCache.TryGetValue(leaveTypeId, out var cached))
                return cached;
            var isPaid = await dbContext.LeaveTypes.Where(lt => lt.Id == leaveTypeId).Select(lt => (bool?)lt.IsPaid).FirstOrDefaultAsync(cancellationToken) ?? true;
            leaveTypeIsPaidCache[leaveTypeId] = isPaid;
            return isPaid;
        }

        var uncoveredHours = 0m;
        var unpaidLeaveHours = 0m;

        for (var date = run.PayPeriodStart.Date; date <= run.PayPeriodEnd.Date; date = date.AddDays(1))
        {
            var dateOffset = new DateTimeOffset(date, TimeSpan.Zero);

            var coveringLeave = approvedLeaves.FirstOrDefault(r => r.DateFrom.Date <= date && r.DateTo.Date >= date);
            if (coveringLeave is not null)
            {
                if (!await IsPaidAsync(coveringLeave.LeaveTypeId))
                    unpaidLeaveHours += HoursPerDay;
                continue; // paid leave day is already fully accounted for by the LeaveRequest's own approval
            }

            var dtr = await dbContext.DailyTimeRecords
                .Where(d => d.EmployeeId == employee.Id && d.Date.Date == date)
                .Select(d => new { d.TimeIn, d.TimeOut })
                .FirstOrDefaultAsync(cancellationToken);

            // DtrComputationService already resolves whether this date is even a scheduled work day
            // for the employee (via the effective EmployeeScheduleAssignment) — a non-work-day or a
            // day with no covering assignment correctly comes back as "not applicable" (IsAbsent
            // false, minutes null), contributing nothing here, without duplicating that logic.
            var computed = await DtrComputationService.ComputeAsync(dbContext, employee.Id, dateOffset, dtr?.TimeIn, dtr?.TimeOut, cancellationToken);
            if (computed.IsAbsent)
                uncoveredHours += HoursPerDay;
            else
                uncoveredHours += ((computed.LateMinutes ?? 0) + (computed.UndertimeMinutes ?? 0)) / 60m;
        }

        var neededLeaveDays = uncoveredHours / HoursPerDay;
        var leaveDaysConsumed = 0m;
        Guid? leaveTypeConsumedId = null;

        if (neededLeaveDays > 0)
        {
            var candidateLeaveTypeIds = await dbContext.LeaveTypes
                .Where(lt => lt.Status == "active" && lt.IsPaid)
                .Select(lt => lt.Id)
                .ToListAsync(cancellationToken);

            var bestBalance = 0m;
            Guid? bestLeaveTypeId = null;
            foreach (var leaveTypeId in candidateLeaveTypeIds)
            {
                var balance = await dbContext.LeaveBalanceEntries
                    .Where(e => e.EmployeeId == employee.Id && e.LeaveTypeId == leaveTypeId)
                    .OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAt)
                    .Select(e => (decimal?)e.RunningBalance)
                    .FirstOrDefaultAsync(cancellationToken) ?? 0m;
                if (balance > bestBalance)
                {
                    bestBalance = balance;
                    bestLeaveTypeId = leaveTypeId;
                }
            }

            if (bestLeaveTypeId is not null)
            {
                leaveDaysConsumed = Round(Math.Min(neededLeaveDays, bestBalance));
                leaveTypeConsumedId = bestLeaveTypeId;
            }
        }

        var remainingHours = (neededLeaveDays - leaveDaysConsumed) * HoursPerDay + unpaidLeaveHours;
        var hourlyRate = employee.BasicSalary / WorkingDaysPerMonth / HoursPerDay;
        var absenceDeduction = remainingHours > 0 ? Round(remainingHours * hourlyRate) : 0m;

        return (leaveDaysConsumed, leaveTypeConsumedId, absenceDeduction);
    }

    private static async Task<decimal> ComputeOvertimePayAsync(IAppDbContext dbContext, Employee employee, PayrollRun run, CancellationToken cancellationToken)
    {
        var approvedRequests = await dbContext.OvertimeRequests
            .Where(o => o.EmployeeId == employee.Id && o.Status == "APPROVED" && o.Date >= run.PayPeriodStart && o.Date <= run.PayPeriodEnd)
            .ToListAsync(cancellationToken);
        if (approvedRequests.Count == 0)
            return 0m;

        var hourlyRate = employee.BasicSalary / WorkingDaysPerMonth / HoursPerDay;
        var total = 0m;

        foreach (var request in approvedRequests)
        {
            var requestedHours = (decimal)(request.PlannedTimeTo - request.PlannedTimeFrom).TotalHours;
            if (requestedHours <= 0)
                continue;

            var scheduledTimeOut = await GetEffectiveScheduledTimeOutAsync(dbContext, employee.Id, request.Date, cancellationToken);
            var dtr = await dbContext.DailyTimeRecords
                .Where(d => d.EmployeeId == employee.Id && d.Date.Date == request.Date.Date)
                .Select(d => new { d.TimeOut })
                .FirstOrDefaultAsync(cancellationToken);

            var actualHours = 0m;
            if (scheduledTimeOut is not null && dtr?.TimeOut is not null && dtr.TimeOut.Value > scheduledTimeOut.Value)
                actualHours = (decimal)(dtr.TimeOut.Value - scheduledTimeOut.Value).TotalHours;

            var payableHours = Math.Min(requestedHours, actualHours);
            if (payableHours <= 0)
                continue;

            var rule = await dbContext.OvertimeRateRules
                .Where(r => r.OtType == request.OtType && r.Status == "active" && r.EffectiveDate <= request.Date)
                .OrderByDescending(r => r.EffectiveDate)
                .FirstOrDefaultAsync(cancellationToken);
            if (rule is null)
                continue; // no rate seeded yet for this OT category — can't invent a multiplier, pays 0 rather than guessing

            total += Round(payableHours * hourlyRate * rule.Multiplier);
        }

        return total;
    }

    private static async Task<DateTimeOffset?> GetEffectiveScheduledTimeOutAsync(IAppDbContext dbContext, Guid employeeId, DateTimeOffset date, CancellationToken cancellationToken)
    {
        var assignment = await dbContext.EmployeeScheduleAssignments
            .Where(a => a.EmployeeId == employeeId && a.EffectiveDate <= date && (a.EndDate == null || a.EndDate >= date))
            .Select(a => new { a.WorkScheduleId })
            .FirstOrDefaultAsync(cancellationToken);
        if (assignment is null)
            return null;

        var schedule = await dbContext.WorkSchedules
            .Where(w => w.Id == assignment.WorkScheduleId)
            .Select(w => new { w.TimeOut })
            .FirstOrDefaultAsync(cancellationToken);
        if (schedule is null)
            return null;

        // DateTimeOffset.Date returns a plain DateTime with the offset stripped — adding a TimeSpan
        // to it and letting it implicitly convert back to DateTimeOffset would reinterpret it in the
        // machine's LOCAL time zone instead of preserving `date`'s own offset. Rebuild explicitly.
        return new DateTimeOffset(date.Date, date.Offset) + schedule.TimeOut.ToTimeSpan();
    }

    private static async Task<(decimal EmployeeShare, decimal EmployerShare)> ComputeSssAsync(IAppDbContext dbContext, Employee employee, PayrollRun run, CancellationToken cancellationToken)
    {
        var bracket = await dbContext.SssContributionBrackets
            .Where(b => b.Status == "active" && b.EffectiveDate <= run.PayDate
                && b.SalaryFrom <= employee.BasicSalary && employee.BasicSalary < b.SalaryTo)
            .OrderByDescending(b => b.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);
        if (bracket is null)
            return (0m, 0m);

        return (Round(bracket.EmployeeShare / 2m), Round(bracket.EmployerShare / 2m));
    }

    /// <summary>
    /// Fixed-peso amount per salary bracket, same shape and lookup as SSS — Pag-IBIG was originally
    /// modeled as percentage-based alongside PhilHealth in GovernmentContributionRate; corrected
    /// once the cooperative confirmed it's actually a fixed amount (see PagIbigContributionBracket).
    /// </summary>
    private static async Task<(decimal EmployeeShare, decimal EmployerShare)> ComputePagIbigAsync(IAppDbContext dbContext, Employee employee, PayrollRun run, CancellationToken cancellationToken)
    {
        var bracket = await dbContext.PagIbigContributionBrackets
            .Where(b => b.Status == "active" && b.EffectiveDate <= run.PayDate
                && b.SalaryFrom <= employee.BasicSalary && employee.BasicSalary < b.SalaryTo)
            .OrderByDescending(b => b.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);
        if (bracket is null)
            return (0m, 0m);

        return (Round(bracket.EmployeeShare / 2m), Round(bracket.EmployerShare / 2m));
    }

    private static async Task<(decimal EmployeeShare, decimal EmployerShare)> ComputeGovernmentPctAsync(
        IAppDbContext dbContext, Employee employee, PayrollRun run, string contributionType, CancellationToken cancellationToken)
    {
        var rate = await dbContext.GovernmentContributionRates
            .Where(r => r.ContributionType == contributionType && r.Status == "active" && r.EffectiveDate <= run.PayDate
                && r.SalaryFrom <= employee.BasicSalary && employee.BasicSalary < r.SalaryTo)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);
        if (rate is null)
            return (0m, 0m);

        var monthlyEmployeeShare = employee.BasicSalary * rate.EmployeeSharePct / 100m;
        var monthlyEmployerShare = employee.BasicSalary * rate.EmployerSharePct / 100m;
        if (rate.MaxContribution is { } max)
        {
            monthlyEmployeeShare = Math.Min(monthlyEmployeeShare, max);
            monthlyEmployerShare = Math.Min(monthlyEmployerShare, max);
        }

        return (Round(monthlyEmployeeShare / 2m), Round(monthlyEmployerShare / 2m));
    }

    private static async Task<decimal> ComputeWithholdingTaxAsync(IAppDbContext dbContext, decimal taxableIncome, PayrollRun run, CancellationToken cancellationToken)
    {
        if (taxableIncome <= 0)
            return 0m;

        var bracket = await dbContext.WithholdingTaxBrackets
            .Where(b => b.Status == "active" && b.EffectiveDate <= run.PayDate
                && b.SalaryFrom <= taxableIncome && taxableIncome < b.SalaryTo)
            .OrderByDescending(b => b.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);
        if (bracket is null)
            return 0m;

        return Round(bracket.BaseTax + bracket.RateOverExcess * (taxableIncome - bracket.SalaryFrom));
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
