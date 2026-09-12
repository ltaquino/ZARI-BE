namespace ZARI.Application.UnitTests.Features.Accounting.GlJournal;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Accounting.GlJournals.Reverse;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.UnitTests.TestSupport;

public sealed class ReverseGlJournalsCommandHandlerTests
{
    private static ReverseGlJournalsCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db) =>
        new(db, new GetNextDocumentNumberCommandHandler(db));

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, string branchId, Guid debitId, Guid creditId)> SeedWithPostedJournal()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var debitAccount = LoanTestFixtures.GlAccount(code: "1000");
        var creditAccount = LoanTestFixtures.GlAccount(code: "2000");
        db.GlAccounts.AddRange(debitAccount, creditAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var postHandler = new PostGlJournalCommandHandler(db, new GetNextDocumentNumberCommandHandler(db));
        var postResult = await postHandler.HandleAsync(
            new PostGlJournalCommand(branch.Id, DateTimeOffset.UtcNow, "TEST", "TestDocument", "doc-1", "Original",
            [
                new PostGlJournalLineInput(debitAccount.Id, null, 1000, 0, null),
                new PostGlJournalLineInput(creditAccount.Id, null, 0, 1000, null)
            ]),
            TestContext.Current.CancellationToken);
        postResult.IsSuccess.Should().BeTrue();

        return (db, branch.Id, debitAccount.Id, creditAccount.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Post_A_Swapped_Reversal_And_Flag_Original_Reversed()
    {
        var (db, _, _, _) = await SeedWithPostedJournal();

        var result = await Handler(db).HandleAsync(new ReverseGlJournalsCommand("TestDocument", "doc-1", DateTimeOffset.UtcNow, null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        var reversal = result.Value![0];
        reversal.Lines.Should().Contain(l => l.DebitAmount == 1000);
        reversal.Lines.Should().Contain(l => l.CreditAmount == 1000);
        (await db.GlJournals.CountAsync(j => j.Status == "REVERSED", TestContext.Current.CancellationToken)).Should().Be(1);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Empty_When_Nothing_Was_Ever_Posted()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await Handler(db).HandleAsync(new ReverseGlJournalsCommand("TestDocument", "doc-missing", DateTimeOffset.UtcNow, null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_FiscalYear_Covering_The_Reversal_Date_Is_Closed()
    {
        var (db, _, _, _) = await SeedWithPostedJournal();
        var reversalDate = new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero);
        db.FiscalYears.Add(AccountingTestFixtures.FiscalYear(
            yearName: "FY2026", startDate: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            endDate: new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero), status: "CLOSED"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new ReverseGlJournalsCommand("TestDocument", "doc-1", reversalDate, null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlJournal.PeriodClosed");
        await db.DisposeAsync();
    }
}
