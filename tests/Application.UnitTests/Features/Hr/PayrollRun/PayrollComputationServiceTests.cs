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
        db.GovernmentContributionRates.Add(HrTestFixtures.GovernmentContributionRate(contributionType: "PAGIBIG", salaryFrom: 0m, salaryTo: 100000m, employeeSharePct: 1m, employerSharePct: 2m, effectiveDate: PayrollTestHelpers.PayPeriodStart.AddYears(-1)));
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
