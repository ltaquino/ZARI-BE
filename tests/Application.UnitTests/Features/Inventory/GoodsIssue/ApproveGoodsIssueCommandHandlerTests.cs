namespace ZARI.Application.UnitTests.Features.Inventory.GoodsIssue;

using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Inventory.GoodsIssues.Approve;
using ZARI.Application.Features.Inventory.SerialNumbers.Issue;
using ZARI.Application.Features.Inventory.StockLedgers.Issue;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// The real success path is not InMemory-testable: IssueStockLinesCommand opens a real
/// transaction (faked here so guard clauses and the resume-after-partial-approve branch can be
/// exercised), and the handler's own final status flip uses ExecuteUpdateAsync (also unsupported).
/// Every dependency is faked; only guard clauses and the ApprovalRequest re-check are exercised.
/// </summary>
public sealed class ApproveGoodsIssueCommandHandlerTests
{
    private static ApproveGoodsIssueCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db,
            LoanTestFixtures.SuccessHandler<IssueStockLinesCommand, Result<IssueStockLinesResponse>>(Result.Success(new IssueStockLinesResponse(new Dictionary<string, decimal>()))),
            LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(Dummy())),
            LoanTestFixtures.SuccessHandler<IssueSerialCommand, Result>(Result.Failure(Error.Failure("x", "unused"))),
            LoanTestFixtures.SuccessHandler<PostGlJournalCommand, Result<GlJournalResponse>>(Result.Failure<GlJournalResponse>(Error.Failure("x", "unused"))),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "GOODS_ISSUE", "x", "br-1", "manager", DateTimeOffset.UtcNow, "APPROVED", "SUBMIT", null, [], DateTimeOffset.UtcNow);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, GoodsIssue issue, Branch branch)> Seed(string status = "PENDING_APPROVAL", bool withApprovalRequest = true, string approvalRequestStatus = "PENDING")
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var issue = InventoryTestFixtures.GoodsIssue(branch.Id, warehouse.Id, item.Id, uom.Id, status: status);
        db.GoodsIssues.Add(issue);
        if (withApprovalRequest)
        {
            db.ApprovalRequests.Add(new ApprovalRequest
            {
                EntityType = "GOODS_ISSUE", EntityId = issue.Id.ToString(), BranchId = branch.Id,
                RequestedBy = "encoder", RequestedAt = DateTimeOffset.UtcNow, Status = approvalRequestStatus, RequestType = "SUBMIT"
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, issue, branch);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new ApproveGoodsIssueCommand(Guid.NewGuid(), "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, issue, branch) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("GOODS_ISSUES", FormAction.Approve, branch.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new ApproveGoodsIssueCommand(issue.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Approval()
    {
        var (db, issue, _) = await Seed(status: "DRAFT");

        var result = await Handler(db).HandleAsync(new ApproveGoodsIssueCommand(issue.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsIssue.NotPendingApproval");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Approval_Request_Found()
    {
        var (db, issue, _) = await Seed(withApprovalRequest: false);

        var result = await Handler(db).HandleAsync(new ApproveGoodsIssueCommand(issue.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApprovalRequest.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Request_Already_Decided_As_Rejected()
    {
        var (db, issue, _) = await Seed(approvalRequestStatus: "REJECTED");

        var result = await Handler(db).HandleAsync(new ApproveGoodsIssueCommand(issue.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsIssue.RequestAlreadyDecided");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Resume_Into_Posting_When_Request_Already_Approved()
    {
        // A prior Approve attempt got as far as deciding (APPROVED) but failed during posting —
        // this resumes straight into the posting steps instead of re-deciding or erroring. With
        // IssueStockLinesCommand faked to a no-cost success, execution reaches the real
        // PostInventoryJournalAsync code, which needs a GL account that's not seeded here — proving
        // the resume branch, not the decide branch, was taken.
        var (db, issue, _) = await Seed(approvalRequestStatus: "APPROVED");

        var result = await Handler(db).HandleAsync(new ApproveGoodsIssueCommand(issue.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlAccount.NotFound");
        await db.DisposeAsync();
    }
}
