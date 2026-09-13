namespace ZARI.Application.UnitTests.Features.Inventory.GoodsIssue;

using ZARI.Application.Features.Inventory.GoodsIssues.Submit;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class SubmitGoodsIssueCommandHandlerTests
{
    private static SubmitGoodsIssueCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db,
            LoanTestFixtures.SuccessHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>>(Result.Success(Dummy())),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "GOODS_ISSUE", "x", "br-1", "encoder", DateTimeOffset.UtcNow, "PENDING", "SUBMIT", null, [], DateTimeOffset.UtcNow);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.GoodsIssue issue, ZARI.Domain.Entities.Branch branch)> Seed(string status = "DRAFT", bool withLines = true)
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
        if (!withLines)
            issue.Lines.Clear();
        db.GoodsIssues.Add(issue);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, issue, branch);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new SubmitGoodsIssueCommand(Guid.NewGuid(), "encoder"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, issue, branch) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("GOODS_ISSUES", FormAction.Edit, branch.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new SubmitGoodsIssueCommand(issue.Id, "encoder"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, issue, _) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(new SubmitGoodsIssueCommand(issue.Id, "encoder"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsIssue.NotDraft");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Lines()
    {
        var (db, issue, _) = await Seed(withLines: false);

        var result = await Handler(db).HandleAsync(new SubmitGoodsIssueCommand(issue.Id, "encoder"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsIssue.NoLines");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Submit_For_Approval()
    {
        var (db, issue, _) = await Seed();

        var result = await Handler(db).HandleAsync(new SubmitGoodsIssueCommand(issue.Id, "encoder"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("PENDING_APPROVAL");
        await db.DisposeAsync();
    }
}
