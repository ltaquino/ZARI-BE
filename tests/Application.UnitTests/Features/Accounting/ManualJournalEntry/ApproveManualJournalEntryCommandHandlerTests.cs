namespace ZARI.Application.UnitTests.Features.Accounting.ManualJournalEntry;

using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Accounting.ManualJournalEntries.Approve;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Unlike most Loan Approve handlers, this one's own status flip is a plain tracked-entity
/// SaveChangesAsync (not ExecuteUpdateAsync) — only the injected DecideApprovalRequestCommand
/// dependency uses ExecuteUpdateAsync internally, so faking just that one dependency (as every
/// other Approve handler's tests already do) unlocks full success-path coverage here, including a
/// real PostGlJournalCommandHandler actually posting a GlJournal.
/// </summary>
public sealed class ApproveManualJournalEntryCommandHandlerTests
{
    private static ApproveManualJournalEntryCommandHandler Handler(
        ZARI.Infrastructure.Persistence.AppDbContext db,
        ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null,
        ApprovalRequestResponse? decideResponse = null) =>
        new(db, LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(decideResponse ?? Dummy())),
            new PostGlJournalCommandHandler(db, new GetNextDocumentNumberCommandHandler(db)),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "MANUAL_JOURNAL_ENTRY", "x", "br-1", "user", DateTimeOffset.UtcNow, "APPROVED", "SUBMIT", null, [], DateTimeOffset.UtcNow);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ManualJournalEntry entry)> Seed(string status = "PENDING_APPROVAL", bool withApprovalRequest = true)
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var debit = LoanTestFixtures.GlAccount(code: "6000");
        var credit = LoanTestFixtures.GlAccount(code: "2000");
        db.GlAccounts.AddRange(debit, credit);
        var entry = AccountingTestFixtures.ManualJournalEntry(branch.Id, debit.Id, credit.Id, status: status);
        db.ManualJournalEntries.Add(entry);
        if (withApprovalRequest)
        {
            db.ApprovalRequests.Add(new ApprovalRequest
            {
                EntityType = "MANUAL_JOURNAL_ENTRY", EntityId = entry.Id.ToString(), BranchId = branch.Id,
                RequestedBy = "encoder", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT"
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, entry);
    }

    [Fact]
    public async Task HandleAsync_Should_Approve_And_Post_A_Real_GlJournal()
    {
        var (db, entry) = await Seed();

        var result = await Handler(db).HandleAsync(new ApproveManualJournalEntryCommand(entry.Id, "manager", "ok"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("POSTED");
        db.GlJournals.Should().ContainSingle(j => j.SourceReferenceTable == "ManualJournalEntry" && j.SourceReferenceId == entry.Id.ToString());
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new ApproveManualJournalEntryCommand(Guid.NewGuid(), "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, entry) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("MANUAL_JOURNAL_ENTRIES", FormAction.Approve, entry.BranchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new ApproveManualJournalEntryCommand(entry.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Approval()
    {
        var (db, entry) = await Seed(status: "DRAFT");

        var result = await Handler(db).HandleAsync(new ApproveManualJournalEntryCommand(entry.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ManualJournalEntry.NotPendingApproval");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Approval_Request_Found()
    {
        var (db, entry) = await Seed(withApprovalRequest: false);

        var result = await Handler(db).HandleAsync(new ApproveManualJournalEntryCommand(entry.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApprovalRequest.NotFound");
        await db.DisposeAsync();
    }
}
