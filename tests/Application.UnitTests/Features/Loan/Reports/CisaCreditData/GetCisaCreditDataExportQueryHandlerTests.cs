namespace ZARI.Application.UnitTests.Features.Loan.Reports.CisaCreditData;

using ZARI.Application.Features.Loan.Reports.CisaCreditData;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetCisaCreditDataExportQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Empty_When_No_Accounts()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetCisaCreditDataExportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetCisaCreditDataExportQuery(null, null, null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Include_Ledger_Balance_And_WriteOff_And_Restructuring_Flags()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        var customer = LoanTestFixtures.Customer(branch.Id);
        var product = LoanTestFixtures.LoanProduct();
        db.Branches.Add(branch);
        db.Customers.Add(customer);
        db.LoanProducts.Add(product);
        var account = LoanTestFixtures.LoanAccount(branch.Id, customer.Id, product.Id, status: "WRITTEN_OFF", withSchedule: false);
        db.LoanAccounts.Add(account);
        db.LoanLedgerEntries.Add(new LoanLedgerEntry
        {
            LoanAccountId = account.Id, SequenceNo = 1, EntryDate = DateTimeOffset.UtcNow, TransactionType = "DISBURSEMENT",
            ReferenceTable = "LoanDisbursement", ReferenceId = "ref-1", PrincipalIn = 12000, PrincipalOut = 0,
            RunningPrincipalBalance = 12000, IsReversal = false, PostedAt = DateTimeOffset.UtcNow
        });
        var expenseAccount = LoanTestFixtures.GlAccount(code: "6910", name: "Loan Write-off Expense");
        db.GlAccounts.Add(expenseAccount);
        var writeOffDate = DateTimeOffset.UtcNow.AddDays(-5);
        db.LoanWriteOffs.Add(new LoanWriteOff
        {
            WriteOffNo = "LOAN-WO-0001", BranchId = branch.Id, LoanAccountId = account.Id, WriteOffDate = writeOffDate,
            Amount = 12000, WriteOffExpenseAccountId = expenseAccount.Id, Reason = "x", Status = "POSTED"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetCisaCreditDataExportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetCisaCreditDataExportQuery(null, null, null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        var row = result.Value![0];
        row.OutstandingPrincipalBalance.Should().Be(12000);
        row.WriteOffDate.Should().Be(writeOffDate);
        row.WasRestructured.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_Should_Leave_WriteOffDate_Null_When_Never_Written_Off()
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
        var handler = new GetCisaCreditDataExportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetCisaCreditDataExportQuery(null, null, null), TestContext.Current.CancellationToken);

        result.Value![0].WriteOffDate.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_Summarize_Negative_Credit_Records_Per_Customer()
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
        db.CustomerCreditRecords.Add(new CustomerCreditRecord
        {
            CustomerId = customer.Id, RecordType = "DEFAULT", Description = "defaulted on a prior coop loan", RecordDate = DateTimeOffset.UtcNow.AddYears(-1)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetCisaCreditDataExportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetCisaCreditDataExportQuery(null, null, null), TestContext.Current.CancellationToken);

        result.Value![0].NegativeCreditRecordsSummary.Should().Contain("DEFAULT").And.Contain("defaulted on a prior coop loan");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("LOAN_ACCOUNTS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetCisaCreditDataExportQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetCisaCreditDataExportQuery(null, null, null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
