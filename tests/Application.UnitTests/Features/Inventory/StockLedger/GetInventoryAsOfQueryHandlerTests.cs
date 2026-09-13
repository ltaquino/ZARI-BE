namespace ZARI.Application.UnitTests.Features.Inventory.StockLedger;

using ZARI.Application.Features.Inventory.StockLedgers.GetInventoryAsOf;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Entities;

public sealed class GetInventoryAsOfQueryHandlerTests
{
    private static StockLedger Ledger(Guid itemId, string branchId, Guid warehouseId, DateTimeOffset transactionDate, DateTimeOffset postedAt, decimal runningQty, decimal runningValue, string? itemCode = "ITEM-1") => new()
    {
        ItemId = itemId, ItemCode = itemCode, BranchId = branchId, WarehouseId = warehouseId, TransactionType = "GOODS_RECEIPT",
        ReferenceTable = "GoodsReceiptPoLine", ReferenceId = Guid.NewGuid().ToString(), QtyIn = runningQty, QtyOut = 0, UnitCost = 10,
        RunningBalanceQty = runningQty, RunningBalanceValue = runningValue, IsReversal = false,
        TransactionDate = transactionDate, PostedAt = postedAt
    };

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ITEMS", ZARI.Domain.Common.FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetInventoryAsOfQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetInventoryAsOfQuery(DateTimeOffset.UtcNow, null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ZARI.Domain.Common.ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Take_The_Last_Row_Per_Item_Warehouse_Batch_As_Of_The_Cutoff()
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
        var asOfDate = DateTimeOffset.UtcNow;
        db.StockLedgers.Add(Ledger(item.Id, branch.Id, warehouse.Id, asOfDate.AddDays(-2), asOfDate.AddDays(-2), 10, 100));
        db.StockLedgers.Add(Ledger(item.Id, branch.Id, warehouse.Id, asOfDate.AddDays(-1), asOfDate.AddDays(-1), 15, 150));
        // Posted after the as-of cutoff — must be excluded.
        db.StockLedgers.Add(Ledger(item.Id, branch.Id, warehouse.Id, asOfDate.AddDays(5), asOfDate.AddDays(5), 25, 250));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetInventoryAsOfQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetInventoryAsOfQuery(asOfDate, null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value!.Single();
        row.QtyOnHand.Should().Be(15);
        row.TotalValue.Should().Be(150);
        row.AvgUnitCost.Should().Be(10);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Exclude_Zero_Balance_Rows_By_Default()
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
        var asOfDate = DateTimeOffset.UtcNow;
        db.StockLedgers.Add(Ledger(item.Id, branch.Id, warehouse.Id, asOfDate, asOfDate, 0, 0));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetInventoryAsOfQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetInventoryAsOfQuery(asOfDate, null), TestContext.Current.CancellationToken);

        result.Value.Should().BeEmpty();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Include_Zero_Balance_Rows_When_Requested()
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
        var asOfDate = DateTimeOffset.UtcNow;
        db.StockLedgers.Add(Ledger(item.Id, branch.Id, warehouse.Id, asOfDate, asOfDate, 0, 0));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetInventoryAsOfQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetInventoryAsOfQuery(asOfDate, null, IncludeZero: true), TestContext.Current.CancellationToken);

        result.Value.Should().ContainSingle();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Filter_By_Branch()
    {
        await using var db = TestDbContextFactory.Create();
        var branch1 = LoanTestFixtures.Branch(id: "br-1");
        var branch2 = LoanTestFixtures.Branch(id: "br-2");
        db.Branches.AddRange(branch1, branch2);
        var warehouse = InventoryTestFixtures.Warehouse(branch1.Id);
        db.Warehouses.Add(warehouse);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var asOfDate = DateTimeOffset.UtcNow;
        db.StockLedgers.Add(Ledger(item.Id, branch1.Id, warehouse.Id, asOfDate, asOfDate, 10, 100));
        db.StockLedgers.Add(Ledger(item.Id, branch2.Id, warehouse.Id, asOfDate, asOfDate, 20, 200));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetInventoryAsOfQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetInventoryAsOfQuery(asOfDate, branch1.Id), TestContext.Current.CancellationToken);

        result.Value.Should().ContainSingle(r => r.BranchId == branch1.Id);
        await db.DisposeAsync();
    }
}
