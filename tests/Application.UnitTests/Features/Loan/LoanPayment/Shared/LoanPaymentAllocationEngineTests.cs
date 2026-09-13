namespace ZARI.Application.UnitTests.Features.Loan.LoanPayment.Shared;

using ZARI.Application.Features.Loan.LoanPayments.Shared;
using ZARI.Domain.Entities;

public sealed class LoanPaymentAllocationEngineTests
{
    private static LoanAmortizationScheduleLine Line(int installmentNo, DateTimeOffset dueDate, decimal principalDue = 1000, decimal interestDue = 100, string status = "DUE") => new()
    {
        InstallmentNo = installmentNo, DueDate = dueDate, PrincipalDue = principalDue, InterestDue = interestDue, TotalDue = principalDue + interestDue,
        PrincipalPaid = 0, InterestPaid = 0, PenaltyPaid = 0, Status = status
    };

    [Fact]
    public void Allocate_Should_Apply_Interest_Before_Principal_On_One_Installment()
    {
        var line = Line(1, DateTimeOffset.UtcNow.AddDays(30));

        var result = LoanPaymentAllocationEngine.Allocate([line], DateTimeOffset.UtcNow, 500, penaltyRatePct: 2, gracePeriodDays: 5);

        result.Lines.Should().HaveCount(1);
        result.Lines[0].InterestApplied.Should().Be(100);
        result.Lines[0].PrincipalApplied.Should().Be(400);
        line.Status.Should().Be("PARTIALLY_PAID");
    }

    [Fact]
    public void Allocate_Should_Pay_Oldest_Installment_First()
    {
        var line1 = Line(1, DateTimeOffset.UtcNow.AddDays(-30));
        var line2 = Line(2, DateTimeOffset.UtcNow.AddDays(30));

        var result = LoanPaymentAllocationEngine.Allocate([line2, line1], DateTimeOffset.UtcNow, 1100, penaltyRatePct: 0, gracePeriodDays: 5);

        result.Lines.Should().HaveCount(1);
        result.Lines[0].Line.InstallmentNo.Should().Be(1);
        line1.Status.Should().Be("PAID");
        line2.Status.Should().Be("DUE");
    }

    [Fact]
    public void Allocate_Should_Cascade_Prepayment_Into_Future_Installments()
    {
        var line1 = Line(1, DateTimeOffset.UtcNow.AddDays(-30));
        var line2 = Line(2, DateTimeOffset.UtcNow.AddDays(0));
        var line3 = Line(3, DateTimeOffset.UtcNow.AddDays(30));

        // covers installment 1 (1100) + installment 2 (1100) fully, plus 550 toward installment 3.
        var result = LoanPaymentAllocationEngine.Allocate([line1, line2, line3], DateTimeOffset.UtcNow, 2750, penaltyRatePct: 0, gracePeriodDays: 5);

        line1.Status.Should().Be("PAID");
        line2.Status.Should().Be("PAID");
        line3.Status.Should().Be("PARTIALLY_PAID");
        result.PrincipalTotal.Should().Be(2450);
        result.InterestTotal.Should().Be(300);
    }

    [Fact]
    public void Allocate_Should_Skip_Lines_Already_Fully_Paid()
    {
        var paidLine = Line(1, DateTimeOffset.UtcNow.AddDays(-60), status: "PAID");
        var dueLine = Line(2, DateTimeOffset.UtcNow.AddDays(30));

        var result = LoanPaymentAllocationEngine.Allocate([paidLine, dueLine], DateTimeOffset.UtcNow, 500, penaltyRatePct: 2, gracePeriodDays: 5);

        result.Lines.Should().ContainSingle(l => l.Line.InstallmentNo == 2);
    }

    [Fact]
    public void Allocate_Should_Compute_TotalOutstanding_Across_Entire_Schedule_Regardless_Of_Amount()
    {
        var line1 = Line(1, DateTimeOffset.UtcNow.AddDays(-30));
        var line2 = Line(2, DateTimeOffset.UtcNow.AddDays(30));

        var result = LoanPaymentAllocationEngine.Allocate([line1, line2], DateTimeOffset.UtcNow, 1, penaltyRatePct: 0, gracePeriodDays: 5);

        result.TotalOutstanding.Should().Be(2200);
    }

    [Fact]
    public void Allocate_Should_Apply_Penalty_Before_Interest_And_Principal_When_Overdue()
    {
        var overdueDate = DateTimeOffset.UtcNow.AddDays(-30);
        var line = Line(1, overdueDate);

        var result = LoanPaymentAllocationEngine.Allocate([line], DateTimeOffset.UtcNow, 1100, penaltyRatePct: 2, gracePeriodDays: 5);

        result.PenaltyTotal.Should().BeGreaterThan(0);
        result.Lines[0].PenaltyApplied.Should().Be(result.PenaltyTotal);
        (result.PrincipalTotal + result.InterestTotal).Should().Be(1100 - result.PenaltyTotal);
    }
}
