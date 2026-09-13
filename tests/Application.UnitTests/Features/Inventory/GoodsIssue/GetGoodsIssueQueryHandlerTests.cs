namespace ZARI.Application.UnitTests.Features.Inventory.GoodsIssue;

using ZARI.Application.Features.Inventory.GoodsIssues.Get;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetGoodsIssueQueryHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.GoodsIssue issue, ZARI.Domain.Entities.Branch branch, ZARI.Domain.Entities.Branch destBranch)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var destBranch = LoanTestFixtures.Branch("br-2");
        db.Branches.Add(destBranch);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        var destWarehouse = InventoryTestFixtures.Warehouse(destBranch.Id, code: "WH2");
        db.Warehouses.Add(destWarehouse);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var issue = InventoryTestFixtures.GoodsIssue(branch.Id, warehouse.Id, item.Id, uom.Id, referenceType: "STOCK_TRANSFER", destBranchId: destBranch.Id, destWarehouseId: destWarehouse.Id);
        db.GoodsIssues.Add(issue);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, issue, branch, destBranch);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetGoodsIssueQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetGoodsIssueQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden_On_Both_Source_And_Dest()
    {
        var (db, issue, branch, destBranch) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("GOODS_ISSUES", FormAction.View, branch.Id, Arg.Any<CancellationToken>()).Returns(false);
        permissions.HasPermissionOnBranchAsync("GOODS_ISSUES", FormAction.View, destBranch.Id, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetGoodsIssueQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetGoodsIssueQuery(issue.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Succeed_When_Only_Dest_Branch_Permission_Granted()
    {
        // Proves the dual-branch OR-check: a user with visibility only at the destination branch
        // (e.g. staff receiving an interbranch transfer, no access to the source branch) can still
        // view the issue.
        var (db, issue, branch, destBranch) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("GOODS_ISSUES", FormAction.View, branch.Id, Arg.Any<CancellationToken>()).Returns(false);
        permissions.HasPermissionOnBranchAsync("GOODS_ISSUES", FormAction.View, destBranch.Id, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new GetGoodsIssueQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetGoodsIssueQuery(issue.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Issue()
    {
        var (db, issue, _, _) = await Seed();
        var handler = new GetGoodsIssueQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetGoodsIssueQuery(issue.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(issue.Id);
        result.Value.Lines.Should().ContainSingle();
        await db.DisposeAsync();
    }
}
