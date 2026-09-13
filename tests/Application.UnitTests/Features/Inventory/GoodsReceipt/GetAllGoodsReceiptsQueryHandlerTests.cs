namespace ZARI.Application.UnitTests.Features.Inventory.GoodsReceipt;

using ZARI.Application.Features.Inventory.GoodsReceipts.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetAllGoodsReceiptsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("GOODS_RECEIPTS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllGoodsReceiptsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllGoodsReceiptsQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Receipts_Ordered_By_Date_Descending()
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
        var older = InventoryTestFixtures.GoodsReceipt(branch.Id, warehouse.Id, item.Id, uom.Id);
        older.GrDate = DateTimeOffset.UtcNow.AddDays(-5);
        var newer = InventoryTestFixtures.GoodsReceipt(branch.Id, warehouse.Id, item.Id, uom.Id);
        newer.GrDate = DateTimeOffset.UtcNow;
        db.GoodsReceipts.AddRange(older, newer);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllGoodsReceiptsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllGoodsReceiptsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].Id.Should().Be(newer.Id);
    }
}
