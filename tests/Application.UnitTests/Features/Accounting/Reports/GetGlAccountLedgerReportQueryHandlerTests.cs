namespace ZARI.Application.UnitTests.Features.Accounting.Reports;

using ZARI.Application.Features.Accounting.Reports.GlAccountLedger;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetGlAccountLedgerReportQueryHandlerTests
{
    private static GlJournal Journal(string branchId, Guid debitAccountId, Guid creditAccountId, decimal amount, DateTimeOffset date) => new()
    {
        JournalNo = $"JV-{Guid.NewGuid():N}", BranchId = branchId, JournalDate = date, SourceModule = "TEST",
        SourceReferenceTable = "Test", SourceReferenceId = "doc-1", Status = "POSTED",
        Lines =
        [
            new GlJournalLine { AccountId = debitAccountId, DebitAmount = amount, CreditAmount = 0 },
            new GlJournalLine { AccountId = creditAccountId, DebitAmount = 0, CreditAmount = amount }
        ]
    };

    [Fact]
    public async Task HandleAsync_Should_Compute_Opening_Balance_And_Running_Balance()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var cash = LoanTestFixtures.GlAccount(code: "1000", normalBalance: "Debit");
        var ap = LoanTestFixtures.GlAccount(code: "2000", normalBalance: "Credit");
        db.GlAccounts.AddRange(cash, ap);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var today = DateTimeOffset.UtcNow.Date;
        db.GlJournals.Add(Journal(branch.Id, cash.Id, ap.Id, 500, today.AddDays(-10))); // before the "from" window
        db.GlJournals.Add(Journal(branch.Id, cash.Id, ap.Id, 300, today)); // inside the window
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetGlAccountLedgerReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetGlAccountLedgerReportQuery(cash.Id, null, today.AddDays(-1), today.AddDays(1)), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Opening.Should().Be(500);
        result.Value!.Lines.Should().ContainSingle();
        result.Value!.Lines[0].RunningBalance.Should().Be(800);
        result.Value!.Closing.Should().Be(800);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Account_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetGlAccountLedgerReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetGlAccountLedgerReportQuery(Guid.NewGuid(), null, null, null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("GL_JOURNALS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetGlAccountLedgerReportQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetGlAccountLedgerReportQuery(Guid.NewGuid(), null, null, null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
