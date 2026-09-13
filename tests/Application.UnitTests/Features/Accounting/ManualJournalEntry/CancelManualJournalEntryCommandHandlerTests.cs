namespace ZARI.Application.UnitTests.Features.Accounting.ManualJournalEntry;

using ZARI.Application.Features.Accounting.ManualJournalEntries.Cancel;
using ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>CancelPendingApprovalRequestCommandHandler uses ExecuteUpdateAsync internally, so it's faked here — the same InMemory gap as every other Approve/Cancel dependency in this suite.</summary>
public sealed class CancelManualJournalEntryCommandHandlerTests
{
    private static CancelManualJournalEntryCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, LoanTestFixtures.SuccessHandler<CancelPendingApprovalRequestCommand, Result>(Result.Success()),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ManualJournalEntry entry)> Seed(string status = "DRAFT")
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
    public async Task HandleAsync_Should_Cancel_Draft_Entry()
    {
        var (db, entry) = await Seed();

        var result = await Handler(db).HandleAsync(new CancelManualJournalEntryCommand(entry.Id, "encoder", "no longer needed"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("CANCELLED");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new CancelManualJournalEntryCommand(Guid.NewGuid(), "encoder", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, entry) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("MANUAL_JOURNAL_ENTRIES", FormAction.Cancel, entry.BranchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new CancelManualJournalEntryCommand(entry.Id, "encoder", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Already_Cancelled()
    {
        var (db, entry) = await Seed(status: "CANCELLED");

        var result = await Handler(db).HandleAsync(new CancelManualJournalEntryCommand(entry.Id, "encoder", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ManualJournalEntry.AlreadyCancelled");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Posted()
    {
        var (db, entry) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(new CancelManualJournalEntryCommand(entry.Id, "encoder", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ManualJournalEntry.RequiresCancellationRequest");
        await db.DisposeAsync();
    }
}
