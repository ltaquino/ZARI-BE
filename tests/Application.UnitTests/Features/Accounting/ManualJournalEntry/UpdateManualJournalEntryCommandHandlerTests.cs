namespace ZARI.Application.UnitTests.Features.Accounting.ManualJournalEntry;

using ZARI.Application.Features.Accounting.ManualJournalEntries.Create;
using ZARI.Application.Features.Accounting.ManualJournalEntries.Update;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class UpdateManualJournalEntryCommandHandlerTests
{
    private static UpdateManualJournalEntryCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static UpdateManualJournalEntryCommand Command(Guid id, Guid debitId, Guid creditId, decimal amount = 500) =>
        new(id, DateTimeOffset.UtcNow, "Updated remarks", "admin",
        [
            new ManualJournalEntryLineInput(debitId, null, null, amount, 0),
            new ManualJournalEntryLineInput(creditId, null, null, 0, amount)
        ]);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, Guid debitId, Guid creditId, ZARI.Domain.Entities.ManualJournalEntry entry)> Seed(string status = "DRAFT")
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
        return (db, debit.Id, credit.Id, entry);
    }

    [Fact]
    public async Task HandleAsync_Should_Update_Draft_Entry()
    {
        var (db, debitId, creditId, entry) = await Seed();

        var result = await Handler(db).HandleAsync(Command(entry.Id, debitId, creditId, 500), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Remarks.Should().Be("Updated remarks");
        result.Value!.Lines.Should().Contain(l => l.DebitAmount == 500);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        var (db, debitId, creditId, _) = await Seed();

        var result = await Handler(db).HandleAsync(Command(Guid.NewGuid(), debitId, creditId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, debitId, creditId, entry) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("MANUAL_JOURNAL_ENTRIES", FormAction.Edit, entry.BranchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(Command(entry.Id, debitId, creditId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, debitId, creditId, entry) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(Command(entry.Id, debitId, creditId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ManualJournalEntry.NotDraft");
        await db.DisposeAsync();
    }
}
