namespace ZARI.Application.UnitTests.Features.Accounting.ManualJournalEntry;

using ZARI.Application.Features.Accounting.ManualJournalEntries.Delete;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class DeleteManualJournalEntryCommandHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.ManualJournalEntry entry)> Seed(string status = "DRAFT")
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var debit = LoanTestFixtures.GlAccount(code: "6000");
        var credit = LoanTestFixtures.GlAccount(code: "2000");
        db.GlAccounts.AddRange(debit, credit);
        var entry = AccountingTestFixtures.ManualJournalEntry(branch.Id, debit.Id, credit.Id, status: status);
        db.ManualJournalEntries.Add(entry);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, entry);
    }

    [Fact]
    public async Task HandleAsync_Should_Delete_Draft_Entry()
    {
        var (db, entry) = await Seed();
        var handler = new DeleteManualJournalEntryCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteManualJournalEntryCommand(entry.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.ManualJournalEntries.Should().BeEmpty();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteManualJournalEntryCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteManualJournalEntryCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, entry) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("MANUAL_JOURNAL_ENTRIES", FormAction.Delete, entry.BranchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteManualJournalEntryCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeleteManualJournalEntryCommand(entry.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, entry) = await Seed(status: "POSTED");
        var handler = new DeleteManualJournalEntryCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteManualJournalEntryCommand(entry.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ManualJournalEntry.NotDraft");
        await db.DisposeAsync();
    }
}
