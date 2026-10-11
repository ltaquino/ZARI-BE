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

    [Fact]
    public async Task HandleAsync_Should_Filter_By_Branch_And_Return_Only_Rows_Changed_Since()
    {
        await using var db = TestDbContextFactory.Create();
        var ct = TestContext.Current.CancellationToken;
        var branch = LoanTestFixtures.Branch();
        var other = LoanTestFixtures.Branch("br-2");
        db.Branches.AddRange(branch, other);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        var otherWarehouse = InventoryTestFixtures.Warehouse(other.Id);
        db.Warehouses.AddRange(warehouse, otherWarehouse);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(ct);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(ct);
        var untouched = new StockBalance { ItemId = item.Id, BranchId = branch.Id, WarehouseId = warehouse.Id, BatchNo = "OLD", QtyOnHand = 1 };
        var changedLater = new StockBalance { ItemId = item.Id, BranchId = branch.Id, WarehouseId = warehouse.Id, BatchNo = "MOVED", QtyOnHand = 1 };
        db.StockBalances.AddRange(untouched, changedLater);
        await db.SaveChangesAsync(ct);
        await Task.Delay(20, ct);
        var cut = DateTimeOffset.UtcNow;
        await Task.Delay(20, ct);
        changedLater.QtyOnHand = 3;
        db.StockBalances.AddRange(
            new StockBalance { ItemId = item.Id, BranchId = branch.Id, WarehouseId = warehouse.Id, BatchNo = "NEW", QtyOnHand = 1 },
            new StockBalance { ItemId = item.Id, BranchId = other.Id, WarehouseId = otherWarehouse.Id, BatchNo = "OTHER", QtyOnHand = 1 });
        await db.SaveChangesAsync(ct);
        var handler = new ListStockBalancesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var delta = await handler.HandleAsync(new ListStockBalancesQuery(branch.Id, cut), ct);
        var branchOnly = await handler.HandleAsync(new ListStockBalancesQuery(branch.Id), ct);

        delta.Value!.Select(b => b.BatchNo).Should().BeEquivalentTo(["MOVED", "NEW"]);
        branchOnly.Value!.Select(b => b.BatchNo).Should().BeEquivalentTo(["OLD", "MOVED", "NEW"]);
    }
}
