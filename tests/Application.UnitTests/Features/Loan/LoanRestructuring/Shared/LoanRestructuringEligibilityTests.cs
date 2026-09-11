namespace ZARI.Application.UnitTests.Features.Loan.LoanRestructuring.Shared;

using ZARI.Application.Features.Loan.LoanRestructurings.Shared;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Entities;

public sealed class LoanRestructuringEligibilityTests
{
    private static LoanAccount Account(params LoanAmortizationScheduleLine[] lines)
    {
        var account = LoanTestFixtures.LoanAccount("br-1", Guid.NewGuid(), Guid.NewGuid(), withSchedule: false);
        account.ScheduleLines.AddRange(lines);
        return account;
    }

    private static LoanAmortizationScheduleLine Line(int installmentNo, DateTimeOffset dueDate, decimal principalDue = 1000, decimal interestDue = 100,
        decimal principalPaid = 0, decimal interestPaid = 0, string status = "DUE") => new()
    {
        InstallmentNo = installmentNo, DueDate = dueDate, PrincipalDue = principalDue, InterestDue = interestDue, TotalDue = principalDue + interestDue,
        PrincipalPaid = principalPaid, InterestPaid = interestPaid, PenaltyPaid = 0, Status = status
    };

    [Fact]
    public void ComputeUnpaidInterestAndPenalty_Should_Be_Zero_When_No_Installment_Is_Due_Yet()
    {
        var asOfDate = DateTimeOffset.UtcNow;
        var account = Account(Line(1, asOfDate.AddDays(30)));

        var unpaid = LoanRestructuringEligibility.ComputeUnpaidInterestAndPenalty(account, asOfDate);

        unpaid.Should().Be(0m);
    }

    [Fact]
    public void ComputeUnpaidInterestAndPenalty_Should_Sum_Unpaid_Interest_On_Due_Installments()
    {
        var asOfDate = DateTimeOffset.UtcNow;
        var account = Account(Line(1, asOfDate.AddDays(-10), interestDue: 100, interestPaid: 0));

        var unpaid = LoanRestructuringEligibility.ComputeUnpaidInterestAndPenalty(account, asOfDate);

        unpaid.Should().BeGreaterThanOrEqualTo(100m);
    }

    [Fact]
    public void ComputeUnpaidInterestAndPenalty_Should_Ignore_Paid_And_Superseded_Lines()
    {
        var asOfDate = DateTimeOffset.UtcNow;
        var account = Account(
            Line(1, asOfDate.AddDays(-30), interestDue: 100, interestPaid: 100, status: "PAID"),
            Line(2, asOfDate.AddDays(-10), interestDue: 100, interestPaid: 0, status: "SUPERSEDED"));

        var unpaid = LoanRestructuringEligibility.ComputeUnpaidInterestAndPenalty(account, asOfDate);

        unpaid.Should().Be(0m);
    }

    [Fact]
    public void ComputeUnpaidInterestAndPenalty_Should_Ignore_FutureInstallments_Interest()
    {
        var asOfDate = DateTimeOffset.UtcNow;
        var account = Account(
            Line(1, asOfDate.AddDays(-10), interestDue: 100, interestPaid: 100, status: "PAID"),
            Line(2, asOfDate.AddDays(30), interestDue: 100, interestPaid: 0));

        var unpaid = LoanRestructuringEligibility.ComputeUnpaidInterestAndPenalty(account, asOfDate);

        unpaid.Should().Be(0m);
    }

    [Fact]
    public async Task GetCurrentPrincipalBalanceAsync_Should_Return_Latest_Ledger_Balance()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        var customer = LoanTestFixtures.Customer(branch.Id);
        var product = LoanTestFixtures.LoanProduct();
        db.Branches.Add(branch);
        db.Customers.Add(customer);
        db.LoanProducts.Add(product);
        var account = LoanTestFixtures.LoanAccount(branch.Id, customer.Id, product.Id, withSchedule: false);
        db.LoanAccounts.Add(account);
        db.LoanLedgerEntries.Add(new ZARI.Domain.Entities.LoanLedgerEntry
        {
            LoanAccountId = account.Id, SequenceNo = 1, EntryDate = DateTimeOffset.UtcNow, TransactionType = "DISBURSEMENT",
            ReferenceTable = "LoanDisbursement", ReferenceId = "ref-1", PrincipalIn = 12000, PrincipalOut = 0,
            RunningPrincipalBalance = 12000, IsReversal = false, PostedAt = DateTimeOffset.UtcNow
        });
        db.LoanLedgerEntries.Add(new ZARI.Domain.Entities.LoanLedgerEntry
        {
            LoanAccountId = account.Id, SequenceNo = 2, EntryDate = DateTimeOffset.UtcNow, TransactionType = "PAYMENT",
            ReferenceTable = "LoanPayment", ReferenceId = "ref-2", PrincipalIn = 0, PrincipalOut = 2000,
            RunningPrincipalBalance = 10000, IsReversal = false, PostedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var balance = await LoanRestructuringEligibility.GetCurrentPrincipalBalanceAsync(db, account.Id, TestContext.Current.CancellationToken);

        balance.Should().Be(10000);
    }

    [Fact]
    public async Task GetCurrentPrincipalBalanceAsync_Should_Return_Zero_When_No_Ledger_Entries()
    {
        await using var db = TestDbContextFactory.Create();

        var balance = await LoanRestructuringEligibility.GetCurrentPrincipalBalanceAsync(db, Guid.NewGuid(), TestContext.Current.CancellationToken);

        balance.Should().Be(0);
    }
}
