namespace ZARI.Application.UnitTests.Features.Inventory.GoodsIssue;

using ZARI.Application.Features.Inventory.GoodsIssues.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetAllGoodsIssuesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("GOODS_ISSUES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllGoodsIssuesQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllGoodsIssuesQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Issues_Ordered_By_Date_Descending()
    {
        await using var db = TestDbContextFactory.Create();
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
        var older = InventoryTestFixtures.GoodsIssue(branch.Id, warehouse.Id, item.Id, uom.Id);
        older.GiDate = DateTimeOffset.UtcNow.AddDays(-5);
        var newer = InventoryTestFixtures.GoodsIssue(branch.Id, warehouse.Id, item.Id, uom.Id);
        newer.GiDate = DateTimeOffset.UtcNow;
        db.GoodsIssues.AddRange(older, newer);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllGoodsIssuesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllGoodsIssuesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].Id.Should().Be(newer.Id);
    }
}
