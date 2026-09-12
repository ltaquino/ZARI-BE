namespace ZARI.Application.UnitTests.Features.Accounting.ManualJournalEntry;

using ZARI.Application.Features.Accounting.ManualJournalEntries.Create;
using ZARI.Application.Features.Accounting.ManualJournalEntries.GetAll;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateManualJournalEntryCommandHandlerTests
{
    private static CreateManualJournalEntryCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new GetNextDocumentNumberCommandHandler(db), new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static CreateManualJournalEntryCommand Command(string branchId, Guid debitId, Guid creditId, decimal amount = 1000) =>
        new(branchId, DateTimeOffset.UtcNow, "Accrual", "admin",
        [
            new ManualJournalEntryLineInput(debitId, null, "Debit", amount, 0),
            new ManualJournalEntryLineInput(creditId, null, "Credit", 0, amount)
        ]);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, string branchId, Guid debitId, Guid creditId)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var debit = LoanTestFixtures.GlAccount(code: "6000");
        var credit = LoanTestFixtures.GlAccount(code: "2000");
        db.GlAccounts.AddRange(debit, credit);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch.Id, debit.Id, credit.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Create_DraftEntry()
    {
        var (db, branchId, debitId, creditId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, debitId, creditId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
        result.Value!.Lines.Should().HaveCount(2);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branchId, debitId, creditId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("MANUAL_JOURNAL_ENTRIES", FormAction.Create, branchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(Command(branchId, debitId, creditId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        var (db, _, debitId, creditId) = await Seed();

        var result = await Handler(db).HandleAsync(Command("br-missing", debitId, creditId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
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

    [Fact]
    public async Task HandleAsync_Should_Fail_When_CostCenter_Not_Found()
    {
        var (db, branchId, debitId, creditId) = await Seed();
        var command = Command(branchId, debitId, creditId) with
        {
            Lines = [new ManualJournalEntryLineInput(debitId, Guid.NewGuid(), null, 1000, 0), new ManualJournalEntryLineInput(creditId, null, null, 0, 1000)]
        };

        var result = await Handler(db).HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CostCenter.NotFound");
        await db.DisposeAsync();
    }
}
