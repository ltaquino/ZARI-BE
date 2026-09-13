namespace ZARI.Application.UnitTests.Features.Inventory.StockOpname;

using ZARI.Application.Features.Inventory.StockOpnames.GetAllPaged;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetAllStockOpnamesPagedQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("STOCK_OPNAMES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllStockOpnamesPagedQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllStockOpnamesPagedQuery(), TestContext.Current.CancellationToken);

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
        var match = InventoryTestFixtures.StockOpname(branch.Id, warehouse.Id, item.Id);
        match.OpnameNo = "OPN-MATCH-1";
        var noMatch = InventoryTestFixtures.StockOpname(branch.Id, warehouse.Id, item.Id);
        noMatch.OpnameNo = "OPN-OTHER-1";
        db.StockOpnames.AddRange(match, noMatch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllStockOpnamesPagedQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllStockOpnamesPagedQuery(1, 20, "MATCH"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle(o => o.Id == match.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Empty_Page_When_No_Opnames_Exist()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetAllStockOpnamesPagedQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllStockOpnamesPagedQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }
}
