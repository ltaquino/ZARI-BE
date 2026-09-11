namespace ZARI.Application.UnitTests.Features.Loan.Reports.CdaPortfolioQuality;

using ZARI.Application.Features.Loan.Reports.CdaPortfolioQuality;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetCdaPortfolioQualityReportQueryHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, LoanAccount account)> SeedAccount(
        int daysOverdue, decimal balance, bool isDisputed = false)
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        var customer = LoanTestFixtures.Customer(branch.Id);
        var product = LoanTestFixtures.LoanProduct();
        db.Branches.Add(branch);
        db.Customers.Add(customer);
        db.LoanProducts.Add(product);
        var account = LoanTestFixtures.LoanAccount(branch.Id, customer.Id, product.Id, status: "ACTIVE", withSchedule: false);
        account.IsDisputed = isDisputed;
        account.ScheduleLines.Add(new LoanAmortizationScheduleLine
        {
            InstallmentNo = 1, DueDate = DateTimeOffset.UtcNow.AddDays(-daysOverdue), PrincipalDue = 1000, InterestDue = 100, TotalDue = 1100,
            OutstandingPrincipalAfter = balance, PrincipalPaid = 0, InterestPaid = 0, PenaltyPaid = 0, Status = "DUE"
        });
        db.LoanAccounts.Add(account);
        db.LoanLedgerEntries.Add(new LoanLedgerEntry
        {
            LoanAccountId = account.Id, SequenceNo = 1, EntryDate = DateTimeOffset.UtcNow, TransactionType = "DISBURSEMENT",
            ReferenceTable = "LoanDisbursement", ReferenceId = "ref-1", PrincipalIn = balance, PrincipalOut = 0,
            RunningPrincipalBalance = balance, IsReversal = false, PostedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, account);
    }

    [Fact]
    public async Task HandleAsync_Should_Classify_Current_When_Not_Overdue()
    {
        var (db, _) = await SeedAccount(daysOverdue: -10, balance: 10000);
        var handler = new GetCdaPortfolioQualityReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetCdaPortfolioQualityReportQuery(null, null), TestContext.Current.CancellationToken);

        result.Value!.Accounts[0].Classification.Should().Be("CURRENT");
        result.Value.Accounts[0].ParAmount.Should().Be(0);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Classify_PastDue_And_Set_Par_To_Entire_Balance()
    {
        var (db, _) = await SeedAccount(daysOverdue: 10, balance: 10000);
        var handler = new GetCdaPortfolioQualityReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetCdaPortfolioQualityReportQuery(null, null), TestContext.Current.CancellationToken);

        var row = result.Value!.Accounts[0];
        row.Classification.Should().Be("PAST_DUE");
        row.ParAmount.Should().Be(10000);
        row.ApllRequiredPct.Should().Be(0.35m);
        row.ApllRequiredAmount.Should().Be(3500);
    }

    [Fact]
    public async Task HandleAsync_Should_Apply_100Pct_Apll_Past_365_Days()
    {
        var (db, _) = await SeedAccount(daysOverdue: 400, balance: 10000);
        var handler = new GetCdaPortfolioQualityReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetCdaPortfolioQualityReportQuery(null, null), TestContext.Current.CancellationToken);

        var row = result.Value!.Accounts[0];
        row.ApllRequiredPct.Should().Be(1.00m);
        row.ApllRequiredAmount.Should().Be(10000);
    }

    [Fact]
    public async Task HandleAsync_Should_Classify_InLitigation_When_Disputed_Regardless_Of_Overdue()
    {
        var (db, _) = await SeedAccount(daysOverdue: -10, balance: 10000, isDisputed: true);
        var handler = new GetCdaPortfolioQualityReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetCdaPortfolioQualityReportQuery(null, null), TestContext.Current.CancellationToken);

        result.Value!.Accounts[0].Classification.Should().Be("IN_LITIGATION");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Classify_Restructured_Account_As_Restructured_When_Not_Yet_Overdue()
    {
        var (db, account) = await SeedAccount(daysOverdue: -10, balance: 10000);
        var oldAccount = LoanTestFixtures.LoanAccount(account.BranchId, account.CustomerId, account.LoanProductId, status: "RESTRUCTURED", withSchedule: false);
        db.LoanAccounts.Add(oldAccount);
        db.LoanRestructurings.Add(new LoanRestructuring
        {
            RestructuringNo = "LOAN-RESTR-0001", BranchId = account.BranchId, OldLoanAccountId = oldAccount.Id, NewLoanAccountId = account.Id,
            RestructureDate = DateTimeOffset.UtcNow, OldPrincipalBalance = 10000, NewAnnualInterestRatePct = 10, NewTermMonths = 12,
            NewRepaymentFrequency = "MONTHLY", NewGracePeriodDays = 5, NewPenaltyRatePct = 2, NewFirstDueDate = DateTimeOffset.UtcNow.AddMonths(1),
            Reason = "x", Status = "POSTED"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetCdaPortfolioQualityReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetCdaPortfolioQualityReportQuery(null, null), TestContext.Current.CancellationToken);

        result.Value!.Accounts.Should().ContainSingle(r => r.LoanAccountId == account.Id && r.Classification == "RESTRUCTURED");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Classify_Restructured_Account_As_PastDue_When_Overdue_With_Zero_Payments()
    {
        var (db, account) = await SeedAccount(daysOverdue: 10, balance: 10000);
        var oldAccount = LoanTestFixtures.LoanAccount(account.BranchId, account.CustomerId, account.LoanProductId, status: "RESTRUCTURED", withSchedule: false);
        db.LoanAccounts.Add(oldAccount);
        db.LoanRestructurings.Add(new LoanRestructuring
        {
            RestructuringNo = "LOAN-RESTR-0001", BranchId = account.BranchId, OldLoanAccountId = oldAccount.Id, NewLoanAccountId = account.Id,
            RestructureDate = DateTimeOffset.UtcNow, OldPrincipalBalance = 10000, NewAnnualInterestRatePct = 10, NewTermMonths = 12,
            NewRepaymentFrequency = "MONTHLY", NewGracePeriodDays = 5, NewPenaltyRatePct = 2, NewFirstDueDate = DateTimeOffset.UtcNow.AddMonths(1),
            Reason = "x", Status = "POSTED"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetCdaPortfolioQualityReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetCdaPortfolioQualityReportQuery(null, null), TestContext.Current.CancellationToken);

        result.Value!.Accounts.Should().ContainSingle(r => r.LoanAccountId == account.Id && r.Classification == "PAST_DUE");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Empty_Response_When_No_Active_Accounts()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetCdaPortfolioQualityReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetCdaPortfolioQualityReportQuery(null, null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Accounts.Should().BeEmpty();
        result.Value.TotalLoansOutstanding.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("LOAN_ACCOUNTS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetCdaPortfolioQualityReportQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetCdaPortfolioQualityReportQuery(null, null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
