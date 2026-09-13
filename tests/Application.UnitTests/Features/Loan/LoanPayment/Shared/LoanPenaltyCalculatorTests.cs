namespace ZARI.Application.UnitTests.Features.Loan.LoanPayment.Shared;

using ZARI.Application.Features.Loan.LoanPayments.Shared;
using ZARI.Domain.Entities;

public sealed class LoanPenaltyCalculatorTests
{
    private static LoanAmortizationScheduleLine Line(DateTimeOffset dueDate, decimal principalDue = 1000, decimal interestDue = 100,
        decimal principalPaid = 0, decimal interestPaid = 0, decimal penaltyPaid = 0) => new()
    {
        InstallmentNo = 1, DueDate = dueDate, PrincipalDue = principalDue, InterestDue = interestDue, TotalDue = principalDue + interestDue,
        PrincipalPaid = principalPaid, InterestPaid = interestPaid, PenaltyPaid = penaltyPaid, Status = "DUE"
    };

    [Fact]
    public void ComputeOwed_Should_Be_Zero_Within_Grace_Period()
    {
        var dueDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var line = Line(dueDate);

        var owed = LoanPenaltyCalculator.ComputeOwed(line, dueDate.AddDays(5), penaltyRatePct: 2, gracePeriodDays: 5);

        owed.Should().Be(0m);
    }

    [Fact]
    public void ComputeOwed_Should_Be_Zero_On_The_Exact_Day_Grace_Period_Ends()
    {
        var dueDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var line = Line(dueDate);

        // gracePeriodDays=5 means penalty starts accruing on dueDate+5; asOfDate == that day is
        // still "on or before", so no penalty yet — the boundary is exclusive of that day.
        var owed = LoanPenaltyCalculator.ComputeOwed(line, dueDate.AddDays(5), penaltyRatePct: 2, gracePeriodDays: 5);

        owed.Should().Be(0m);
    }

    [Fact]
    public void ComputeOwed_Should_Accrue_Daily_After_Grace_Period()
    {
        var dueDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var line = Line(dueDate, principalDue: 1000, interestDue: 100);

        // penalty starts on dueDate+5; 3 days late past that = daysLate=3.
        // outstanding = 1000+100 = 1100; rate 2%/mo pro-rated daily over 30 days.
        var owed = LoanPenaltyCalculator.ComputeOwed(line, dueDate.AddDays(8), penaltyRatePct: 2, gracePeriodDays: 5);

        var expected = Math.Round(1100m * (2m / 100m) * 3 / 30m, 2);
        owed.Should().Be(expected);
    }

    [Fact]
    public void ComputeOwed_Should_Subtract_Already_Paid_Penalty()
    {
        var dueDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var accrued = Math.Round(1100m * (2m / 100m) * 3 / 30m, 2);
        var line = Line(dueDate, principalDue: 1000, interestDue: 100, penaltyPaid: accrued);

        var owed = LoanPenaltyCalculator.ComputeOwed(line, dueDate.AddDays(8), penaltyRatePct: 2, gracePeriodDays: 5);

        owed.Should().Be(0m);
    }

    [Fact]
    public void ComputeOwed_Should_Be_Zero_When_Installment_Fully_Paid()
    {
        var dueDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var line = Line(dueDate, principalDue: 1000, interestDue: 100, principalPaid: 1000, interestPaid: 100);

        var owed = LoanPenaltyCalculator.ComputeOwed(line, dueDate.AddDays(30), penaltyRatePct: 2, gracePeriodDays: 5);

        owed.Should().Be(0m);
    }
}
