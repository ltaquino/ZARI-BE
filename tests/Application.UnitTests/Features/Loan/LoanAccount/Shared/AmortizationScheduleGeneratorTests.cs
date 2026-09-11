namespace ZARI.Application.UnitTests.Features.Loan.LoanAccount.Shared;

using ZARI.Application.Features.Loan.LoanAccounts.Shared;

public sealed class AmortizationScheduleGeneratorTests
{
    [Fact]
    public void Generate_Should_Match_HandVerified_10000_12Pct_12Month_Example()
    {
        var firstDue = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var lines = AmortizationScheduleGenerator.Generate(10000m, 12m, 12, "MONTHLY", firstDue);

        lines.Should().HaveCount(12);
        lines[0].PrincipalDue.Should().BeApproximately(788.49m, 0.01m);
        lines[0].InterestDue.Should().Be(100.00m);
        (lines[0].PrincipalDue + lines[0].InterestDue).Should().BeApproximately(888.49m, 0.01m);
        lines.Sum(l => l.InterestDue).Should().BeApproximately(661.86m, 0.5m);
        lines[^1].OutstandingPrincipalAfter.Should().Be(0m);
    }

    [Fact]
    public void Generate_Should_Fully_Amortize_To_Zero_Regardless_Of_Rounding_Drift()
    {
        var lines = AmortizationScheduleGenerator.Generate(9999.99m, 7.25m, 24, "MONTHLY", DateTimeOffset.UtcNow);

        lines[^1].OutstandingPrincipalAfter.Should().Be(0m);
        lines.Sum(l => l.PrincipalDue).Should().Be(9999.99m);
    }

    [Fact]
    public void Generate_Should_Split_Principal_Evenly_When_Rate_Is_Zero()
    {
        var lines = AmortizationScheduleGenerator.Generate(12000m, 0m, 12, "MONTHLY", DateTimeOffset.UtcNow);

        lines.Should().OnlyContain(l => l.InterestDue == 0m);
        lines.Sum(l => l.PrincipalDue).Should().Be(12000m);
        lines[^1].OutstandingPrincipalAfter.Should().Be(0m);
    }

    [Fact]
    public void Generate_Should_Produce_52_Periods_Per_Year_For_Weekly()
    {
        var firstDue = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var lines = AmortizationScheduleGenerator.Generate(5000m, 12m, 12, "WEEKLY", firstDue);

        lines.Should().HaveCount(52);
        lines[1].DueDate.Should().Be(firstDue.AddDays(7));
        lines[^1].OutstandingPrincipalAfter.Should().Be(0m);
    }

    [Fact]
    public void Generate_Should_Produce_24_Periods_Per_Year_For_SemiMonthly()
    {
        var firstDue = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var lines = AmortizationScheduleGenerator.Generate(6000m, 12m, 12, "SEMI_MONTHLY", firstDue);

        lines.Should().HaveCount(24);
        lines[1].DueDate.Should().Be(firstDue.AddDays(15));
        lines[^1].OutstandingPrincipalAfter.Should().Be(0m);
    }

    [Fact]
    public void Generate_Should_Produce_Single_Installment_When_Term_Is_One_Month()
    {
        var lines = AmortizationScheduleGenerator.Generate(1000m, 12m, 1, "MONTHLY", DateTimeOffset.UtcNow);

        lines.Should().HaveCount(1);
        lines[0].PrincipalDue.Should().Be(1000m);
        lines[0].OutstandingPrincipalAfter.Should().Be(0m);
    }
}
