namespace ZARI.Application.UnitTests.Features.Inventory.StockLedger;

using ZARI.Application.Features.Inventory.StockLedgers.GetBalances;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Entities;

public sealed class ListStockBalancesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ITEMS", ZARI.Domain.Common.FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new ListStockBalancesQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new ListStockBalancesQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ZARI.Domain.Common.ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_All_Balances_Newest_Movement_First()
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
        var older = new StockBalance { ItemId = item.Id, BranchId = branch.Id, WarehouseId = warehouse.Id, QtyOnHand = 5, AvgUnitCost = 10, TotalValue = 50, LastMovementDate = DateTimeOffset.UtcNow.AddDays(-2) };
        var newer = new StockBalance { ItemId = item.Id, BranchId = branch.Id, WarehouseId = warehouse.Id, BatchNo = "B1", QtyOnHand = 8, AvgUnitCost = 20, TotalValue = 160, LastMovementDate = DateTimeOffset.UtcNow };
        db.StockBalances.AddRange(older, newer);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ListStockBalancesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new ListStockBalancesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].BatchNo.Should().Be("B1");
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Empty_When_No_Balances_Exist()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new ListStockBalancesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new ListStockBalancesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
