namespace ZARI.Application.UnitTests.Features.Loan.Reports.CdaPortfolioQuality.Shared;

using ZARI.Application.Features.Loan.Reports.CdaPortfolioQuality.Shared;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Entities;

public sealed class RestructuredLoanGraduationTests
{
    private static LoanAccount Account(string repaymentFrequency, params LoanAmortizationScheduleLine[] lines)
    {
        var account = LoanTestFixtures.LoanAccount("br-1", Guid.NewGuid(), Guid.NewGuid(), repaymentFrequency: repaymentFrequency, withSchedule: false);
        account.ScheduleLines.AddRange(lines);
        return account;
    }

    private static LoanAmortizationScheduleLine Line(int installmentNo, string status, decimal principalPaid = 0, decimal interestPaid = 0) => new()
    {
        InstallmentNo = installmentNo, DueDate = DateTimeOffset.UtcNow, PrincipalDue = 1000, InterestDue = 100, TotalDue = 1100,
        PrincipalPaid = principalPaid, InterestPaid = interestPaid, PenaltyPaid = 0, Status = status
    };

    [Fact]
    public void HasGraduated_Should_Be_True_After_3_Consecutive_Paid_Monthly_Installments()
    {
        var account = Account("MONTHLY",
            Line(1, "PAID", 1000, 100), Line(2, "PAID", 1000, 100), Line(3, "PAID", 1000, 100), Line(4, "DUE"));

        RestructuredLoanGraduation.HasGraduated(account).Should().BeTrue();
    }

    [Fact]
    public void HasGraduated_Should_Be_False_With_Only_2_Consecutive_Paid_Monthly_Installments()
    {
        var account = Account("MONTHLY", Line(1, "PAID", 1000, 100), Line(2, "PAID", 1000, 100), Line(3, "DUE"));

        RestructuredLoanGraduation.HasGraduated(account).Should().BeFalse();
    }

    [Fact]
    public void HasGraduated_Should_Require_Contiguous_Run_From_The_Start()
    {
        // A gap (installment 2 unpaid) breaks the streak even though 3 lines are PAID overall.
        var account = Account("MONTHLY",
            Line(1, "PAID", 1000, 100), Line(2, "PARTIALLY_PAID", 500, 50), Line(3, "PAID", 1000, 100), Line(4, "PAID", 1000, 100));

        RestructuredLoanGraduation.HasGraduated(account).Should().BeFalse();
    }

    [Fact]
    public void HasGraduated_Should_Require_4_Installments_For_SemiMonthly()
    {
        var threePaid = Account("SEMI_MONTHLY", Line(1, "PAID"), Line(2, "PAID"), Line(3, "PAID"), Line(4, "DUE"));
        var fourPaid = Account("SEMI_MONTHLY", Line(1, "PAID"), Line(2, "PAID"), Line(3, "PAID"), Line(4, "PAID"));

        RestructuredLoanGraduation.HasGraduated(threePaid).Should().BeFalse();
        RestructuredLoanGraduation.HasGraduated(fourPaid).Should().BeTrue();
    }

    [Fact]
    public void HasGraduated_Should_Require_6_Installments_For_Weekly()
    {
        var fivePaid = Account("WEEKLY", Line(1, "PAID"), Line(2, "PAID"), Line(3, "PAID"), Line(4, "PAID"), Line(5, "PAID"), Line(6, "DUE"));
        var sixPaid = Account("WEEKLY", Line(1, "PAID"), Line(2, "PAID"), Line(3, "PAID"), Line(4, "PAID"), Line(5, "PAID"), Line(6, "PAID"));

        RestructuredLoanGraduation.HasGraduated(fivePaid).Should().BeFalse();
        RestructuredLoanGraduation.HasGraduated(sixPaid).Should().BeTrue();
    }

    [Fact]
    public void HasAnyPayment_Should_Be_False_When_No_Line_Has_Any_Paid_Amount()
    {
        var account = Account("MONTHLY", Line(1, "DUE"), Line(2, "DUE"));

        RestructuredLoanGraduation.HasAnyPayment(account).Should().BeFalse();
    }

    [Fact]
    public void HasAnyPayment_Should_Be_True_When_Any_Line_Has_Partial_Payment()
    {
        var account = Account("MONTHLY", Line(1, "PARTIALLY_PAID", principalPaid: 200), Line(2, "DUE"));

        RestructuredLoanGraduation.HasAnyPayment(account).Should().BeTrue();
    }
}
