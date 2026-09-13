namespace ZARI.Application.UnitTests.Features.Accounting.ManualJournalEntry;

using ZARI.Application.Features.Accounting.ManualJournalEntries.RejectCancellation;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class RejectManualJournalEntryCancellationCommandHandlerTests
{
    private static RejectManualJournalEntryCancellationCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(Dummy())),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "MANUAL_JOURNAL_ENTRY", "x", "br-1", "user", DateTimeOffset.UtcNow, "REJECTED", "CANCEL", null, [], DateTimeOffset.UtcNow);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ManualJournalEntry entry)> Seed(string status = "PENDING_CANCELLATION", bool withApprovalRequest = true)
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var debit = LoanTestFixtures.GlAccount(code: "6000");
        var credit = LoanTestFixtures.GlAccount(code: "2000");
        db.GlAccounts.AddRange(debit, credit);
        var entry = AccountingTestFixtures.ManualJournalEntry(branch.Id, debit.Id, credit.Id, status: status);
        entry.CancelReason = "pending review";
        db.ManualJournalEntries.Add(entry);
        if (withApprovalRequest)
        {
            db.ApprovalRequests.Add(new ApprovalRequest
            {
                EntityType = "MANUAL_JOURNAL_ENTRY", EntityId = entry.Id.ToString(), BranchId = branch.Id,
                RequestedBy = "manager", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "CANCEL"
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, entry);
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_Cancellation_Back_To_Posted()
    {
        var (db, entry) = await Seed();

        var result = await Handler(db).HandleAsync(new RejectManualJournalEntryCancellationCommand(entry.Id, "admin.hq", "not warranted"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("POSTED");
        result.Value!.CancelReason.Should().BeNull();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new RejectManualJournalEntryCancellationCommand(Guid.NewGuid(), "admin.hq", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, entry) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasCancellationAuthorityAsync("MANUAL_JOURNAL_ENTRIES", Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new RejectManualJournalEntryCancellationCommand(entry.Id, "admin.hq", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Cancellation()
    {
        var (db, entry) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(new RejectManualJournalEntryCancellationCommand(entry.Id, "admin.hq", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ManualJournalEntry.NotPendingCancellation");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Cancellation_Request_Found()
    {
        var (db, entry) = await Seed(withApprovalRequest: false);

        var result = await Handler(db).HandleAsync(new RejectManualJournalEntryCancellationCommand(entry.Id, "admin.hq", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApprovalRequest.NotFound");
        await db.DisposeAsync();
    }
}
