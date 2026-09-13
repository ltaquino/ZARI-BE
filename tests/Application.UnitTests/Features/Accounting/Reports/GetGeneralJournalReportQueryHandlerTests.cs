namespace ZARI.Application.UnitTests.Features.Accounting.Reports;

using ZARI.Application.Features.Accounting.Reports.GeneralJournal;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetGeneralJournalReportQueryHandlerTests
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
    public async Task HandleAsync_Should_Return_Journals_Chronologically_With_Totals()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var cash = LoanTestFixtures.GlAccount(code: "1000", name: "Cash");
        var ap = LoanTestFixtures.GlAccount(code: "2000", name: "AP");
        db.GlAccounts.AddRange(cash, ap);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var today = DateTimeOffset.UtcNow.Date;
        db.GlJournals.Add(Journal(branch.Id, cash.Id, ap.Id, 500, today));
        db.GlJournals.Add(Journal(branch.Id, cash.Id, ap.Id, 300, today.AddDays(-1)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetGeneralJournalReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetGeneralJournalReportQuery(null, null, null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Journals.Should().HaveCount(2);
        result.Value!.Journals[0].JournalDate.Should().BeBefore(result.Value!.Journals[1].JournalDate);
        result.Value!.Journals[0].Lines.Should().Contain(l => l.AccountName == "Cash");
        result.Value!.TotalDebit.Should().Be(800);
        result.Value!.TotalCredit.Should().Be(800);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Filter_By_Date_Range()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var cash = LoanTestFixtures.GlAccount(code: "1000");
        var ap = LoanTestFixtures.GlAccount(code: "2000");
        db.GlAccounts.AddRange(cash, ap);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var today = DateTimeOffset.UtcNow.Date;
        db.GlJournals.Add(Journal(branch.Id, cash.Id, ap.Id, 500, today));
        db.GlJournals.Add(Journal(branch.Id, cash.Id, ap.Id, 300, today.AddDays(-30)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetGeneralJournalReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetGeneralJournalReportQuery(null, today.AddDays(-1), today.AddDays(1)), TestContext.Current.CancellationToken);

        result.Value!.Journals.Should().ContainSingle();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("GL_JOURNALS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetGeneralJournalReportQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetGeneralJournalReportQuery(null, null, null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
