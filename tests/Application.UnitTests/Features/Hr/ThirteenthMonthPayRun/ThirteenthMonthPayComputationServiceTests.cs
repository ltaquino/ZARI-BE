namespace ZARI.Application.UnitTests.Features.Hr.ThirteenthMonthPayRun;

using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Shared;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Infrastructure.Persistence;

/// <summary>
/// Hand-verified per ThirteenthMonthPayComputationService's own header comment: three POSTED
/// PayrollLines of 10000.00, 10000.00, 9500.00 sum to 29500.00; 29500.00 / 12 = 2458.3333... rounds
/// to 2458.33.
/// </summary>
public sealed class ThirteenthMonthPayComputationServiceTests
{
    [Fact]
    public async Task ComputeForEmployeeAsync_Should_Sum_Posted_Lines_For_The_Year_And_Divide_By_Twelve()
    {
        var (db, employee) = await ThirteenthMonthPayRunTestHelpers.SeedBranchAndEmployeeAsync();
        await using var _ = db;
        await ThirteenthMonthPayRunTestHelpers.SeedPostedPayrollAsync(db, employee.Id, 10000.00m, 2026);
        await ThirteenthMonthPayRunTestHelpers.SeedPostedPayrollAsync(db, employee.Id, 10000.00m, 2026);
        await ThirteenthMonthPayRunTestHelpers.SeedPostedPayrollAsync(db, employee.Id, 9500.00m, 2026);

        var (totalBasicPay, thirteenthMonthPay) = await ThirteenthMonthPayComputationService.ComputeForEmployeeAsync(db, employee.Id, 2026, TestContext.Current.CancellationToken);

        totalBasicPay.Should().Be(29500.00m);
        thirteenthMonthPay.Should().Be(2458.33m);
    }

    [Fact]
    public async Task ComputeForEmployeeAsync_Should_Ignore_Non_Posted_Payroll_Lines()
    {
        var (db, employee) = await ThirteenthMonthPayRunTestHelpers.SeedBranchAndEmployeeAsync();
        await using var _ = db;
        var draftRun = HrTestFixtures.PayrollRun(payrollRunNo: "HQ-PAY-DRAFT", status: "DRAFT",
            payPeriodStart: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), payPeriodEnd: new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
        db.PayrollRuns.Add(draftRun);
        db.PayrollLines.Add(HrTestFixtures.PayrollLine(payrollRunId: draftRun.Id, employeeId: employee.Id, basicPay: 10000.00m));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (totalBasicPay, thirteenthMonthPay) = await ThirteenthMonthPayComputationService.ComputeForEmployeeAsync(db, employee.Id, 2026, TestContext.Current.CancellationToken);

        totalBasicPay.Should().Be(0m);
        thirteenthMonthPay.Should().Be(0m);
    }

    [Fact]
    public async Task ComputeForEmployeeAsync_Should_Ignore_Lines_From_A_Different_Year()
    {
        var (db, employee) = await ThirteenthMonthPayRunTestHelpers.SeedBranchAndEmployeeAsync();
        await using var _ = db;
        await ThirteenthMonthPayRunTestHelpers.SeedPostedPayrollAsync(db, employee.Id, 10000.00m, 2025);

        var (totalBasicPay, thirteenthMonthPay) = await ThirteenthMonthPayComputationService.ComputeForEmployeeAsync(db, employee.Id, 2026, TestContext.Current.CancellationToken);

        totalBasicPay.Should().Be(0m);
        thirteenthMonthPay.Should().Be(0m);
    }
}
