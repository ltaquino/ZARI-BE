namespace ZARI.Application.UnitTests.Features.Accounting.Reports;

using ZARI.Application.Features.Accounting.Reports.TrialBalance;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetTrialBalanceReportQueryHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, Branch branch, GlAccount cash, GlAccount ap)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var cash = LoanTestFixtures.GlAccount(code: "1000", name: "Cash", accountType: "Asset", normalBalance: "Debit");
        var ap = LoanTestFixtures.GlAccount(code: "2000", name: "Accounts Payable", accountType: "Liability", normalBalance: "Credit");
        db.GlAccounts.AddRange(cash, ap);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch, cash, ap);
    }

    private static GlJournal Journal(string branchId, Guid debitAccountId, Guid creditAccountId, decimal amount, DateTimeOffset date, string status = "POSTED") => new()
    {
        JournalNo = $"JV-{Guid.NewGuid():N}", BranchId = branchId, JournalDate = date, SourceModule = "TEST",
        SourceReferenceTable = "Test", SourceReferenceId = "doc-1", Status = status,
        Lines =
        [
            new GlJournalLine { AccountId = debitAccountId, DebitAmount = amount, CreditAmount = 0 },
            new GlJournalLine { AccountId = creditAccountId, DebitAmount = 0, CreditAmount = amount }
        ]
    };

    [Fact]
    public async Task HandleAsync_Should_Show_Debit_Balance_For_Asset_And_Credit_Balance_For_Liability()
    {
        var (db, branch, cash, ap) = await Seed();
        db.GlJournals.Add(Journal(branch.Id, cash.Id, ap.Id, 1000, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetTrialBalanceReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetTrialBalanceReportQuery(null, DateTimeOffset.UtcNow), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var cashRow = result.Value!.Rows.Single(r => r.AccountId == cash.Id);
        cashRow.DebitBalance.Should().Be(1000);
        cashRow.CreditBalance.Should().Be(0);
        var apRow = result.Value!.Rows.Single(r => r.AccountId == ap.Id);
        apRow.CreditBalance.Should().Be(1000);
        apRow.DebitBalance.Should().Be(0);
        result.Value!.IsBalanced.Should().BeTrue();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Exclude_Zero_Balance_Accounts_By_Default()
    {
        var (db, _, _, _) = await Seed();
        var handler = new GetTrialBalanceReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetTrialBalanceReportQuery(null, DateTimeOffset.UtcNow), TestContext.Current.CancellationToken);

        result.Value!.Rows.Should().BeEmpty();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Include_Zero_Balance_Accounts_When_Requested()
    {
        var (db, _, _, _) = await Seed();
        var handler = new GetTrialBalanceReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetTrialBalanceReportQuery(null, DateTimeOffset.UtcNow, IncludeZeroBalances: true), TestContext.Current.CancellationToken);

        result.Value!.Rows.Should().HaveCount(2);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Exclude_Journals_Dated_After_AsOfDate()
    {
        var (db, branch, cash, ap) = await Seed();
        db.GlJournals.Add(Journal(branch.Id, cash.Id, ap.Id, 1000, DateTimeOffset.UtcNow.AddDays(5)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetTrialBalanceReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetTrialBalanceReportQuery(null, DateTimeOffset.UtcNow), TestContext.Current.CancellationToken);

        result.Value!.Rows.Should().BeEmpty();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("GL_JOURNALS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetTrialBalanceReportQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetTrialBalanceReportQuery(null, DateTimeOffset.UtcNow), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
