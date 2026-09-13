namespace ZARI.Application.UnitTests.Features.Inventory.GoodsIssue;

using ZARI.Application.Features.Inventory.GoodsIssues.Cancel;
using ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CancelGoodsIssueCommandHandlerTests
{
    private static CancelGoodsIssueCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db,
            LoanTestFixtures.SuccessHandler<CancelPendingApprovalRequestCommand, Result>(Result.Success()),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, GoodsIssue issue, Branch branch)> Seed(string status = "DRAFT")
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
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, issue, branch);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new CancelGoodsIssueCommand(Guid.NewGuid(), "encoder", "mistake"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, issue, branch) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("GOODS_ISSUES", FormAction.Cancel, branch.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new CancelGoodsIssueCommand(issue.Id, "encoder", "mistake"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Already_Cancelled()
    {
        var (db, issue, _) = await Seed(status: "CANCELLED");

        var result = await Handler(db).HandleAsync(new CancelGoodsIssueCommand(issue.Id, "encoder", "mistake"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsIssue.AlreadyCancelled");
        await db.DisposeAsync();
    }

    [Theory]
    [InlineData("POSTED")]
    [InlineData("PENDING_CANCELLATION")]
    public async Task HandleAsync_Should_Fail_When_Requires_Cancellation_Request(string status)
    {
        var (db, issue, _) = await Seed(status: status);

        var result = await Handler(db).HandleAsync(new CancelGoodsIssueCommand(issue.Id, "encoder", "mistake"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsIssue.RequiresCancellationRequest");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Cancel_Draft_Issue()
    {
        var (db, issue, _) = await Seed();

        var result = await Handler(db).HandleAsync(new CancelGoodsIssueCommand(issue.Id, "encoder", "mistake"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("CANCELLED");
        await db.DisposeAsync();
    }
}
