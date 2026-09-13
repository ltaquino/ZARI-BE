namespace ZARI.Application.UnitTests.Features.Accounting.GlJournal;

using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

/// <summary>
/// Every module in this ERP (Loan, Sales, Purchasing, Inventory) posts through this handler, so its
/// coverage matters more than a typical CRUD handler. Uses a real GetNextDocumentNumberCommandHandler
/// rather than a fake — no DocumentSequence is seeded in these tests, so it naturally exercises its
/// own safe timestamp-fallback path (no ExecuteUpdateAsync involved, so no InMemory gap here).
/// </summary>
public sealed class PostGlJournalCommandHandlerTests
{
    private static PostGlJournalCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db) =>
        new(db, new GetNextDocumentNumberCommandHandler(db));

    private static PostGlJournalCommand Command(string branchId, Guid debitAccountId, Guid creditAccountId, decimal amount = 1000, DateTimeOffset? journalDate = null) =>
        new(branchId, journalDate ?? DateTimeOffset.UtcNow, "TEST", "TestDocument", "doc-1", "Test journal",
        [
            new PostGlJournalLineInput(debitAccountId, null, amount, 0, "Debit line"),
            new PostGlJournalLineInput(creditAccountId, null, 0, amount, "Credit line")
        ]);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, string branchId, Guid debitAccountId, Guid creditAccountId)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var debitAccount = LoanTestFixtures.GlAccount(code: "1000");
        var creditAccount = LoanTestFixtures.GlAccount(code: "2000");
        db.GlAccounts.AddRange(debitAccount, creditAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch.Id, debitAccount.Id, creditAccount.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Post_Balanced_Journal()
    {
        var (db, branchId, debitId, creditId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, debitId, creditId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("POSTED");
        result.Value!.Lines.Should().HaveCount(2);
        db.GlJournals.Should().HaveCount(1);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_A_Line_Has_Both_Debit_And_Credit()
    {
        var (db, branchId, debitId, creditId) = await Seed();
        var command = Command(branchId, debitId, creditId) with
        {
            Lines = [new PostGlJournalLineInput(debitId, null, 500, 500, null), new PostGlJournalLineInput(creditId, null, 0, 500, null)]
        };

        var result = await Handler(db).HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlJournal.MalformedLine");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_A_Line_Has_Neither_Debit_Nor_Credit()
    {
        var (db, branchId, debitId, creditId) = await Seed();
        var command = Command(branchId, debitId, creditId) with
        {
            Lines = [new PostGlJournalLineInput(debitId, null, 0, 0, null), new PostGlJournalLineInput(creditId, null, 0, 500, null)]
        };

        var result = await Handler(db).HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlJournal.MalformedLine");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Unbalanced()
    {
        var (db, branchId, debitId, creditId) = await Seed();
        var command = Command(branchId, debitId, creditId) with
        {
            Lines = [new PostGlJournalLineInput(debitId, null, 1000, 0, null), new PostGlJournalLineInput(creditId, null, 0, 900, null)]
        };

        var result = await Handler(db).HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlJournal.Unbalanced");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_FiscalYear_Covering_The_Date_Is_Closed()
    {
        var (db, branchId, debitId, creditId) = await Seed();
        var journalDate = new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero);
        db.FiscalYears.Add(AccountingTestFixtures.FiscalYear(
            yearName: "FY2026", startDate: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            endDate: new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero), status: "CLOSED"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, debitId, creditId, journalDate: journalDate), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlJournal.PeriodClosed");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Succeed_When_FiscalYear_Covering_The_Date_Is_Open()
    {
        var (db, branchId, debitId, creditId) = await Seed();
        var journalDate = new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero);
        db.FiscalYears.Add(AccountingTestFixtures.FiscalYear(
            yearName: "FY2026", startDate: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            endDate: new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero), status: "OPEN"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, debitId, creditId, journalDate: journalDate), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_GlAccount_Not_Found()
    {
        var (db, branchId, debitId, _) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, debitId, Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlAccount.NotFound");
        await db.DisposeAsync();
    }
}
