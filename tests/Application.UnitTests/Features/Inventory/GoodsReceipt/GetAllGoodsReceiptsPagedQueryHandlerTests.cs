namespace ZARI.Application.UnitTests.Features.Inventory.GoodsReceipt;

using ZARI.Application.Features.Inventory.GoodsReceipts.GetAllPaged;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetAllGoodsReceiptsPagedQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("GOODS_RECEIPTS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllGoodsReceiptsPagedQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllGoodsReceiptsPagedQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Filter_By_Search_And_Page()
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
        var match = InventoryTestFixtures.GoodsReceipt(branch.Id, warehouse.Id, item.Id, uom.Id);
        match.GrNo = "GR-MATCH-1";
        var noMatch = InventoryTestFixtures.GoodsReceipt(branch.Id, warehouse.Id, item.Id, uom.Id);
        noMatch.GrNo = "GR-OTHER-1";
        db.GoodsReceipts.AddRange(match, noMatch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllGoodsReceiptsPagedQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllGoodsReceiptsPagedQuery(1, 20, "MATCH"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle(r => r.Id == match.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Empty_Page_When_No_Receipts_Exist()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetAllGoodsReceiptsPagedQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllGoodsReceiptsPagedQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }
}
