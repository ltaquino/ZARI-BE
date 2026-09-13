namespace ZARI.Application.UnitTests.Features.Inventory.StockLocationBalance;

using ZARI.Application.Features.Inventory.StockLocationBalances.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Entities;

public sealed class GetAllStockLocationBalancesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ITEMS", ZARI.Domain.Common.FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllStockLocationBalancesQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllStockLocationBalancesQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ZARI.Domain.Common.ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Exclude_Zero_Balance_Rows()
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
        var location = InventoryTestFixtures.StorageLocation(warehouse.Id);
        db.StorageLocations.Add(location);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.StockLocationBalances.Add(new StockLocationBalance { ItemId = item.Id, WarehouseId = warehouse.Id, LocationId = location.Id, QtyOnHand = 5 });
        db.StockLocationBalances.Add(new StockLocationBalance { ItemId = item.Id, WarehouseId = warehouse.Id, LocationId = location.Id, BatchNo = "EMPTY", QtyOnHand = 0 });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllStockLocationBalancesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllStockLocationBalancesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(b => b.QtyOnHand == 5);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Empty_When_No_Balances_Exist()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetAllStockLocationBalancesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllStockLocationBalancesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
