namespace ZARI.Application.UnitTests.Features.Loan.LoanLedgerEntry;

using ZARI.Application.Features.Loan.LoanLedgerEntries.GetByAccount;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetLoanLedgerEntriesByAccountQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Entries_Ordered_By_SequenceNo()
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
        db.LoanLedgerEntries.Add(new LoanLedgerEntry
        {
            LoanAccountId = account.Id, SequenceNo = 2, EntryDate = DateTimeOffset.UtcNow, TransactionType = "PAYMENT",
            ReferenceTable = "LoanPayment", ReferenceId = "ref-2", PrincipalIn = 0, PrincipalOut = 500,
            RunningPrincipalBalance = 11500, IsReversal = false, PostedAt = DateTimeOffset.UtcNow
        });
        db.LoanLedgerEntries.Add(new LoanLedgerEntry
        {
            LoanAccountId = account.Id, SequenceNo = 1, EntryDate = DateTimeOffset.UtcNow, TransactionType = "DISBURSEMENT",
            ReferenceTable = "LoanDisbursement", ReferenceId = "ref-1", PrincipalIn = 12000, PrincipalOut = 0,
            RunningPrincipalBalance = 12000, IsReversal = false, PostedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetLoanLedgerEntriesByAccountQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetLoanLedgerEntriesByAccountQuery(account.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].SequenceNo.Should().Be(1);
        result.Value[1].SequenceNo.Should().Be(2);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Account_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetLoanLedgerEntriesByAccountQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetLoanLedgerEntriesByAccountQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
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
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LOAN_ACCOUNTS", FormAction.View, account.BranchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetLoanLedgerEntriesByAccountQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetLoanLedgerEntriesByAccountQuery(account.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
