namespace ZARI.Application.UnitTests.Features.Accounting.ManualJournalEntry;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Features.Accounting.GlJournals.Reverse;
using ZARI.Application.Features.Accounting.ManualJournalEntries.ApproveCancellation;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>Uses a real ReverseGlJournalsCommandHandler (no ExecuteUpdateAsync there) and fakes only DecideApprovalRequestCommand.</summary>
public sealed class ApproveManualJournalEntryCancellationCommandHandlerTests
{
    private static ApproveManualJournalEntryCancellationCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new ReverseGlJournalsCommandHandler(db, new GetNextDocumentNumberCommandHandler(db)),
            LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(Dummy())),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "MANUAL_JOURNAL_ENTRY", "x", "br-1", "user", DateTimeOffset.UtcNow, "APPROVED", "CANCEL", null, [], DateTimeOffset.UtcNow);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ManualJournalEntry entry, Guid debitId, Guid creditId)> Seed(
        string status = "PENDING_CANCELLATION", bool withApprovalRequest = true, bool withPostedJournal = true)
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

        if (withPostedJournal)
        {
            db.GlJournals.Add(new GlJournal
            {
                JournalNo = "JV-0001", BranchId = branch.Id, JournalDate = DateTimeOffset.UtcNow, SourceModule = "ACCOUNTING",
                SourceReferenceTable = "ManualJournalEntry", SourceReferenceId = entry.Id.ToString(), Status = "POSTED",
                Lines = [new GlJournalLine { AccountId = debit.Id, DebitAmount = 1000, CreditAmount = 0 }, new GlJournalLine { AccountId = credit.Id, DebitAmount = 0, CreditAmount = 1000 }]
            });
        }
        if (withApprovalRequest)
        {
            db.ApprovalRequests.Add(new ApprovalRequest
            {
                EntityType = "MANUAL_JOURNAL_ENTRY", EntityId = entry.Id.ToString(), BranchId = branch.Id,
                RequestedBy = "manager", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "CANCEL"
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, entry, debit.Id, credit.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Approve_Cancellation_And_Reverse_The_Posted_Journal()
    {
        var (db, entry, _, _) = await Seed();

        var result = await Handler(db).HandleAsync(new ApproveManualJournalEntryCancellationCommand(entry.Id, "admin.hq", "confirmed"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("CANCELLED");
        (await db.GlJournals.CountAsync(j => j.Status == "REVERSED", TestContext.Current.CancellationToken)).Should().Be(1);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new ApproveManualJournalEntryCancellationCommand(Guid.NewGuid(), "admin.hq", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, entry, _, _) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasCancellationAuthorityAsync("MANUAL_JOURNAL_ENTRIES", Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new ApproveManualJournalEntryCancellationCommand(entry.Id, "admin.hq", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Cancellation()
    {
        var (db, entry, _, _) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(new ApproveManualJournalEntryCancellationCommand(entry.Id, "admin.hq", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ManualJournalEntry.NotPendingCancellation");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Cancellation_Request_Found()
    {
        var (db, entry, _, _) = await Seed(withApprovalRequest: false);

        var result = await Handler(db).HandleAsync(new ApproveManualJournalEntryCancellationCommand(entry.Id, "admin.hq", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApprovalRequest.NotFound");
        await db.DisposeAsync();
    }
}
