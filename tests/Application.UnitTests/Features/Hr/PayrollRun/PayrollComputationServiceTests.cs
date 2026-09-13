namespace ZARI.Application.UnitTests.Features.Hr.PayrollRun;

using ZARI.Application.Features.Hr.PayrollRuns.Shared;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Entities;
using ZARI.Infrastructure.Persistence;

/// <summary>
/// Hand-verified against the exact worked samples in PayrollComputationService's own header
/// comment before anything downstream (ComputePayrollRunCommand, the live smoke test) was built on
/// it — same discipline as AmortizationScheduleGeneratorTests for the Loan module.
/// </summary>
public static class PayrollTestHelpers
{
    public static readonly DateTimeOffset PayPeriodStart = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset PayPeriodEnd = new(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset PayDate = new(2026, 1, 16, 0, 0, 0, TimeSpan.Zero);

    public static async Task<Employee> SeedEmployeeAsync(AppDbContext db, decimal basicSalary)
    {
        db.Branches.Add(LoanTestFixtures.Branch());
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        var position = HrTestFixtures.Position(departmentId: department.Id);
        db.Positions.Add(position);
        var employee = HrTestFixtures.Employee(branchId: "br-1", positionId: position.Id, departmentId: department.Id, basicSalary: basicSalary);
        db.Employees.Add(employee);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return employee;
    }

    public static ZARI.Domain.Entities.PayrollRun BuildRun() => HrTestFixtures.PayrollRun(payPeriodStart: PayPeriodStart, payPeriodEnd: PayPeriodEnd, payDate: PayDate);
}

public sealed class PayrollComputationServiceTests
{
    [Fact]
    public async Task ComputeForEmployeeAsync_Should_Match_Hand_Computed_Sample_With_No_Ot_Allowance_Or_Deduction()
    {
        await using var db = TestDbContextFactory.Create();
        var employee = await PayrollTestHelpers.SeedEmployeeAsync(db, basicSalary: 20000m);

        db.SssContributionBrackets.Add(HrTestFixtures.SssContributionBracket(salaryFrom: 0m, salaryTo: 25000m, employeeShare: 500m, employerShare: 1000m, effectiveDate: PayrollTestHelpers.PayPeriodStart.AddYears(-1)));
        db.GovernmentContributionRates.Add(HrTestFixtures.GovernmentContributionRate(contributionType: "PHILHEALTH", salaryFrom: 0m, salaryTo: 100000m, employeeSharePct: 2m, employerSharePct: 2m, effectiveDate: PayrollTestHelpers.PayPeriodStart.AddYears(-1)));
        db.PagIbigContributionBrackets.Add(HrTestFixtures.PagIbigContributionBracket(salaryFrom: 0m, salaryTo: 100000m, employeeShare: 200m, employerShare: 400m, effectiveDate: PayrollTestHelpers.PayPeriodStart.AddYears(-1)));
        db.WithholdingTaxBrackets.Add(HrTestFixtures.WithholdingTaxBracket(salaryFrom: 0m, salaryTo: 20833m, baseTax: 0m, rateOverExcess: 0m, effectiveDate: PayrollTestHelpers.PayPeriodStart.AddYears(-1)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var run = PayrollTestHelpers.BuildRun();

        var draft = await PayrollComputationService.ComputeForEmployeeAsync(db, employee, run, TestContext.Current.CancellationToken);

        draft.BasicPay.Should().Be(10000.00m);
        draft.OvertimePay.Should().Be(0m);
        draft.AllowanceTotal.Should().Be(0m);
        draft.GrossPay.Should().Be(10000.00m);
        draft.SssEmployeeShare.Should().Be(250.00m);
        draft.SssEmployerShare.Should().Be(500.00m);
        draft.PhilHealthEmployeeShare.Should().Be(200.00m);
        draft.PhilHealthEmployerShare.Should().Be(200.00m);
        draft.PagIbigEmployeeShare.Should().Be(100.00m);
        draft.PagIbigEmployerShare.Should().Be(200.00m);
        draft.WithholdingTax.Should().Be(0m);
        draft.DeductionTotal.Should().Be(0m);
        draft.NetPay.Should().Be(9450.00m);
    }

    [Fact]
    public async Task ComputeForEmployeeAsync_Should_Pay_The_Lesser_Of_Requested_And_Dtr_Evidenced_Overtime_Hours()
    {
        await using var db = TestDbContextFactory.Create();
        // BasicSalary 17600 -> hourly rate = 17600 / 22 / 8 = 100.00 exactly.
        var employee = await PayrollTestHelpers.SeedEmployeeAsync(db, basicSalary: 17600m);

        var schedule = HrTestFixtures.WorkSchedule(timeIn: "08:00", timeOut: "17:00", gracePeriodMinutes: 10);
        db.WorkSchedules.Add(schedule);
        db.EmployeeScheduleAssignments.Add(HrTestFixtures.EmployeeScheduleAssignment(employeeId: employee.Id, workScheduleId: schedule.Id, effectiveDate: PayrollTestHelpers.PayPeriodStart.AddYears(-1)));
        db.OvertimeRateRules.Add(HrTestFixtures.OvertimeRateRule(otType: "ORDINARY", multiplier: 1.25m, effectiveDate: PayrollTestHelpers.PayPeriodStart.AddYears(-1)));

        var otDate = new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero); // a Monday within the pay period
        // Requested 2 hours (18:00-20:00); DTR shows clock-out at 20:00 -> 3 actual hours past the
        // 17:00 schedule -> payable = min(2, 3) = 2.
        db.OvertimeRequests.Add(HrTestFixtures.OvertimeRequest(
            employeeId: employee.Id, date: otDate,
            plannedTimeFrom: otDate.AddHours(18), plannedTimeTo: otDate.AddHours(20),
            otType: "ORDINARY", status: "APPROVED"));
        db.DailyTimeRecords.Add(HrTestFixtures.DailyTimeRecord(employeeId: employee.Id, date: otDate, timeIn: otDate.AddHours(8), timeOut: otDate.AddHours(20)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var run = PayrollTestHelpers.BuildRun();

        var draft = await PayrollComputationService.ComputeForEmployeeAsync(db, employee, run, TestContext.Current.CancellationToken);

        // 2 hours * 100.00/hr * 1.25 multiplier = 250.00
        draft.OvertimePay.Should().Be(250.00m);
    }

    [Fact]
    public async Task ComputeForEmployeeAsync_Should_Cap_Overtime_Pay_To_Dtr_Evidenced_Hours_When_Less_Than_Requested()
    {
        await using var db = TestDbContextFactory.Create();
        var employee = await PayrollTestHelpers.SeedEmployeeAsync(db, basicSalary: 17600m);

        var schedule = HrTestFixtures.WorkSchedule(timeIn: "08:00", timeOut: "17:00", gracePeriodMinutes: 10);
        db.WorkSchedules.Add(schedule);
        db.EmployeeScheduleAssignments.Add(HrTestFixtures.EmployeeScheduleAssignment(employeeId: employee.Id, workScheduleId: schedule.Id, effectiveDate: PayrollTestHelpers.PayPeriodStart.AddYears(-1)));
        db.OvertimeRateRules.Add(HrTestFixtures.OvertimeRateRule(otType: "ORDINARY", multiplier: 1.25m, effectiveDate: PayrollTestHelpers.PayPeriodStart.AddYears(-1)));

        var otDate = new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero);
        // Requested 3 hours (18:00-21:00), but DTR shows clock-out at 19:00 -> only 2 actual hours
        // past the 17:00 schedule -> payable = min(3, 2) = 2.
        db.OvertimeRequests.Add(HrTestFixtures.OvertimeRequest(
            employeeId: employee.Id, date: otDate,
            plannedTimeFrom: otDate.AddHours(18), plannedTimeTo: otDate.AddHours(21),
            otType: "ORDINARY", status: "APPROVED"));
        db.DailyTimeRecords.Add(HrTestFixtures.DailyTimeRecord(employeeId: employee.Id, date: otDate, timeIn: otDate.AddHours(8), timeOut: otDate.AddHours(19)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var run = PayrollTestHelpers.BuildRun();

        var draft = await PayrollComputationService.ComputeForEmployeeAsync(db, employee, run, TestContext.Current.CancellationToken);

        draft.OvertimePay.Should().Be(250.00m); // 2h * 100 * 1.25, not the requested 3h
    }

    [Fact]
    public async Task ComputeForEmployeeAsync_Should_Pay_Zero_Overtime_When_No_Rate_Rule_Seeded()
    {
        await using var db = TestDbContextFactory.Create();
        var employee = await PayrollTestHelpers.SeedEmployeeAsync(db, basicSalary: 17600m);

        var schedule = HrTestFixtures.WorkSchedule(timeIn: "08:00", timeOut: "17:00", gracePeriodMinutes: 10);
        db.WorkSchedules.Add(schedule);
        db.EmployeeScheduleAssignments.Add(HrTestFixtures.EmployeeScheduleAssignment(employeeId: employee.Id, workScheduleId: schedule.Id, effectiveDate: PayrollTestHelpers.PayPeriodStart.AddYears(-1)));
        // No OvertimeRateRule seeded at all.

        var otDate = new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero);
        db.OvertimeRequests.Add(HrTestFixtures.OvertimeRequest(
            employeeId: employee.Id, date: otDate,
            plannedTimeFrom: otDate.AddHours(18), plannedTimeTo: otDate.AddHours(20),
            otType: "ORDINARY", status: "APPROVED"));
        db.DailyTimeRecords.Add(HrTestFixtures.DailyTimeRecord(employeeId: employee.Id, date: otDate, timeIn: otDate.AddHours(8), timeOut: otDate.AddHours(20)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var run = PayrollTestHelpers.BuildRun();

        var draft = await PayrollComputationService.ComputeForEmployeeAsync(db, employee, run, TestContext.Current.CancellationToken);

        draft.OvertimePay.Should().Be(0m); // can't invent a multiplier — pays 0 rather than guessing
    }

    [Fact]
    public async Task ComputeForEmployeeAsync_Should_Ignore_NonApproved_Overtime_Requests()
    {
        await using var db = TestDbContextFactory.Create();
        var employee = await PayrollTestHelpers.SeedEmployeeAsync(db, basicSalary: 17600m);
        db.OvertimeRateRules.Add(HrTestFixtures.OvertimeRateRule(otType: "ORDINARY", multiplier: 1.25m, effectiveDate: PayrollTestHelpers.PayPeriodStart.AddYears(-1)));

        var otDate = new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero);
        db.OvertimeRequests.Add(HrTestFixtures.OvertimeRequest(
            employeeId: employee.Id, date: otDate,
            plannedTimeFrom: otDate.AddHours(18), plannedTimeTo: otDate.AddHours(20),
            otType: "ORDINARY", status: "PENDING_APPROVAL"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var run = PayrollTestHelpers.BuildRun();

        var draft = await PayrollComputationService.ComputeForEmployeeAsync(db, employee, run, TestContext.Current.CancellationToken);

        draft.OvertimePay.Should().Be(0m);
    }

    [Fact]
    public async Task ComputeForEmployeeAsync_Should_Include_Effective_Allowances_And_Deductions_In_Full()
    {
        await using var db = TestDbContextFactory.Create();
        var employee = await PayrollTestHelpers.SeedEmployeeAsync(db, basicSalary: 20000m);
        db.EmployeeAllowances.Add(HrTestFixtures.EmployeeAllowance(employeeId: employee.Id, amount: 1000m, effectiveDate: PayrollTestHelpers.PayPeriodStart.AddYears(-1)));
        db.EmployeeDeductions.Add(HrTestFixtures.EmployeeDeduction(employeeId: employee.Id, amount: 500m, effectiveDate: PayrollTestHelpers.PayPeriodStart.AddYears(-1)));
        // An allowance that ended before this run's period must NOT be picked up.
        db.EmployeeAllowances.Add(HrTestFixtures.EmployeeAllowance(employeeId: employee.Id, code: "EXPIRED", amount: 9999m,
            effectiveDate: PayrollTestHelpers.PayPeriodStart.AddYears(-1), endDate: PayrollTestHelpers.PayPeriodStart.AddDays(-1)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var run = PayrollTestHelpers.BuildRun();

        var draft = await PayrollComputationService.ComputeForEmployeeAsync(db, employee, run, TestContext.Current.CancellationToken);

        draft.AllowanceTotal.Should().Be(1000m);
        draft.DeductionTotal.Should().Be(500m);
        draft.GrossPay.Should().Be(11000.00m); // 10000 BasicPay + 1000 allowance
        draft.NetPay.Should().Be(10500.00m); // GrossPay - 500 deduction (no govt brackets seeded in this test)
    }

    [Fact]
    public async Task ComputeForEmployeeAsync_Should_Pay_Zero_Government_Shares_When_No_Bracket_Covers_The_Salary()
    {
        await using var db = TestDbContextFactory.Create();
        var employee = await PayrollTestHelpers.SeedEmployeeAsync(db, basicSalary: 20000m);
        // Bracket exists but tops out below this employee's salary.
        db.SssContributionBrackets.Add(HrTestFixtures.SssContributionBracket(salaryFrom: 0m, salaryTo: 5000m, employeeShare: 100m, employerShare: 200m, effectiveDate: PayrollTestHelpers.PayPeriodStart.AddYears(-1)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var run = PayrollTestHelpers.BuildRun();

        var draft = await PayrollComputationService.ComputeForEmployeeAsync(db, employee, run, TestContext.Current.CancellationToken);

        draft.SssEmployeeShare.Should().Be(0m);
        draft.SssEmployerShare.Should().Be(0m);
    }
}

/// <summary>
/// Absence/late/undertime handling, confirmed with the cooperative 2026-09-13: consume leave
/// balance first, then deduct pay per hour for whatever isn't covered. Each test uses a one-day
/// PayrollRun period (rather than the shared Jan 1-15 helper) so a single absence is isolated
/// without needing to seed DTR "present" rows for every other work day in a longer period.
/// </summary>
public sealed class PayrollComputationServiceAbsenceTests
{
    // A Monday, so the default "MON,TUE,WED,THU,FRI" WorkSchedule always covers it.
    private static readonly DateTimeOffset AbsenceDate = new(2026, 1, 5, 0, 0, 0, TimeSpan.Zero);

    private static ZARI.Domain.Entities.PayrollRun BuildOneDayRun() =>
        HrTestFixtures.PayrollRun(payPeriodStart: AbsenceDate, payPeriodEnd: AbsenceDate, payDate: AbsenceDate.AddDays(1));

    private static async Task SeedScheduleAsync(AppDbContext db, Guid employeeId)
    {
        var schedule = HrTestFixtures.WorkSchedule(timeIn: "08:00", timeOut: "17:00", gracePeriodMinutes: 10);
        db.WorkSchedules.Add(schedule);
        db.EmployeeScheduleAssignments.Add(HrTestFixtures.EmployeeScheduleAssignment(employeeId: employeeId, workScheduleId: schedule.Id, effectiveDate: AbsenceDate.AddYears(-1)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ComputeForEmployeeAsync_Should_Consume_Leave_Balance_For_A_Full_Day_Absence_When_Sufficient()
    {
        await using var db = TestDbContextFactory.Create();
        // BasicSalary 17600 -> hourly rate = 17600 / 22 / 8 = 100.00 exactly.
        var employee = await PayrollTestHelpers.SeedEmployeeAsync(db, basicSalary: 17600m);
        await SeedScheduleAsync(db, employee.Id);

        var leaveType = HrTestFixtures.LeaveType(code: "VL", isPaid: true);
        db.LeaveTypes.Add(leaveType);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await ZARI.Application.Features.Hr.LeaveBalanceEntries.Shared.LeaveBalanceLedgerService.PostEntryAsync(
            db, employee.Id, leaveType.Id, AbsenceDate.AddDays(-1), "ACCRUAL", 5m, null, null, TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        // No DailyTimeRecord for AbsenceDate at all -> IsAbsent.

        var draft = await PayrollComputationService.ComputeForEmployeeAsync(db, employee, BuildOneDayRun(), TestContext.Current.CancellationToken);

        draft.LeaveDaysConsumed.Should().Be(1m);
        draft.LeaveTypeConsumedId.Should().Be(leaveType.Id);
        draft.AbsenceDeduction.Should().Be(0m);
    }

    [Fact]
    public async Task ComputeForEmployeeAsync_Should_Deduct_Pay_For_A_Full_Day_Absence_When_No_Leave_Balance()
    {
        await using var db = TestDbContextFactory.Create();
        var employee = await PayrollTestHelpers.SeedEmployeeAsync(db, basicSalary: 17600m);
        await SeedScheduleAsync(db, employee.Id);
        db.LeaveTypes.Add(HrTestFixtures.LeaveType(code: "VL", isPaid: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        // No LeaveBalanceEntry posted at all -> zero balance.

        var draft = await PayrollComputationService.ComputeForEmployeeAsync(db, employee, BuildOneDayRun(), TestContext.Current.CancellationToken);

        draft.LeaveDaysConsumed.Should().Be(0m);
        draft.LeaveTypeConsumedId.Should().BeNull();
        draft.AbsenceDeduction.Should().Be(800.00m); // 8 hours * 100.00/hr
    }

    [Fact]
    public async Task ComputeForEmployeeAsync_Should_Split_Between_Partial_Leave_Consumption_And_Pay_Deduction()
    {
        await using var db = TestDbContextFactory.Create();
        var employee = await PayrollTestHelpers.SeedEmployeeAsync(db, basicSalary: 17600m);
        await SeedScheduleAsync(db, employee.Id);
        var leaveType = HrTestFixtures.LeaveType(code: "VL", isPaid: true);
        db.LeaveTypes.Add(leaveType);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await ZARI.Application.Features.Hr.LeaveBalanceEntries.Shared.LeaveBalanceLedgerService.PostEntryAsync(
            db, employee.Id, leaveType.Id, AbsenceDate.AddDays(-1), "ACCRUAL", 0.5m, null, null, TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var draft = await PayrollComputationService.ComputeForEmployeeAsync(db, employee, BuildOneDayRun(), TestContext.Current.CancellationToken);

        draft.LeaveDaysConsumed.Should().Be(0.5m);
        draft.LeaveTypeConsumedId.Should().Be(leaveType.Id);
        draft.AbsenceDeduction.Should().Be(400.00m); // remaining 4 hours * 100.00/hr
    }

    [Fact]
    public async Task ComputeForEmployeeAsync_Should_Not_Deduct_Or_Consume_For_A_Day_Already_Covered_By_Approved_Paid_Leave()
    {
        await using var db = TestDbContextFactory.Create();
        var employee = await PayrollTestHelpers.SeedEmployeeAsync(db, basicSalary: 17600m);
        await SeedScheduleAsync(db, employee.Id);
        var leaveType = HrTestFixtures.LeaveType(code: "VL", isPaid: true);
        db.LeaveTypes.Add(leaveType);
        db.LeaveRequests.Add(HrTestFixtures.LeaveRequest(employeeId: employee.Id, leaveTypeId: leaveType.Id, dateFrom: AbsenceDate, dateTo: AbsenceDate, days: 1m, status: "APPROVED"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var draft = await PayrollComputationService.ComputeForEmployeeAsync(db, employee, BuildOneDayRun(), TestContext.Current.CancellationToken);

        draft.LeaveDaysConsumed.Should().Be(0m);
        draft.AbsenceDeduction.Should().Be(0m);
    }

    [Fact]
    public async Task ComputeForEmployeeAsync_Should_Deduct_Full_Day_For_Approved_Unpaid_Leave()
    {
        await using var db = TestDbContextFactory.Create();
        var employee = await PayrollTestHelpers.SeedEmployeeAsync(db, basicSalary: 17600m);
        await SeedScheduleAsync(db, employee.Id);
        var unpaidLeaveType = HrTestFixtures.LeaveType(code: "UL", isPaid: false);
        db.LeaveTypes.Add(unpaidLeaveType);
        db.LeaveRequests.Add(HrTestFixtures.LeaveRequest(employeeId: employee.Id, leaveTypeId: unpaidLeaveType.Id, dateFrom: AbsenceDate, dateTo: AbsenceDate, days: 1m, status: "APPROVED"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var draft = await PayrollComputationService.ComputeForEmployeeAsync(db, employee, BuildOneDayRun(), TestContext.Current.CancellationToken);

        draft.LeaveDaysConsumed.Should().Be(0m); // unpaid leave never draws against a leave balance
        draft.AbsenceDeduction.Should().Be(800.00m); // full day, at the hourly rate
    }

    [Fact]
    public async Task ComputeForEmployeeAsync_Should_Deduct_Only_The_Undertime_Portion_When_Not_Fully_Absent()
    {
        await using var db = TestDbContextFactory.Create();
        var employee = await PayrollTestHelpers.SeedEmployeeAsync(db, basicSalary: 17600m);
        await SeedScheduleAsync(db, employee.Id);
        // Clocks in on time but leaves 2 hours early (15:00 instead of 17:00) -> 120 min undertime.
        db.DailyTimeRecords.Add(HrTestFixtures.DailyTimeRecord(employeeId: employee.Id, date: AbsenceDate, timeIn: AbsenceDate.AddHours(8), timeOut: AbsenceDate.AddHours(15)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var draft = await PayrollComputationService.ComputeForEmployeeAsync(db, employee, BuildOneDayRun(), TestContext.Current.CancellationToken);

        draft.AbsenceDeduction.Should().Be(200.00m); // 2 hours * 100.00/hr — no leave type seeded, so nothing to consume
    }
}
