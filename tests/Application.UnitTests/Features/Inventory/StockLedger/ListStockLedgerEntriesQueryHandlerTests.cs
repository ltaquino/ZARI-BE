namespace ZARI.Application.UnitTests.Features.Inventory.StockLedger;

using ZARI.Application.Features.Inventory.StockLedgers.GetLedgerEntries;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Entities;

public sealed class ListStockLedgerEntriesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ITEMS", ZARI.Domain.Common.FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new ListStockLedgerEntriesQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new ListStockLedgerEntriesQuery(Guid.NewGuid(), Guid.NewGuid(), null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ZARI.Domain.Common.ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Entries_For_The_Given_Item_Warehouse_And_Batch_Ordered_By_PostedAt()
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
        var later = new StockLedger
        {
            ItemId = item.Id, BranchId = branch.Id, WarehouseId = warehouse.Id, TransactionType = "GOODS_ISSUE",
            ReferenceTable = "SalesInvoiceLine", ReferenceId = "ref-2", QtyIn = 0, QtyOut = 3, UnitCost = 10,
            RunningBalanceQty = 7, RunningBalanceValue = 70, IsReversal = false,
            TransactionDate = DateTimeOffset.UtcNow, PostedAt = DateTimeOffset.UtcNow
        };
        var earlier = new StockLedger
        {
            ItemId = item.Id, BranchId = branch.Id, WarehouseId = warehouse.Id, TransactionType = "GOODS_RECEIPT",
            ReferenceTable = "GoodsReceiptPoLine", ReferenceId = "ref-1", QtyIn = 10, QtyOut = 0, UnitCost = 10,
            RunningBalanceQty = 10, RunningBalanceValue = 100, IsReversal = false,
            TransactionDate = DateTimeOffset.UtcNow.AddDays(-1), PostedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        db.StockLedgers.AddRange(later, earlier);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ListStockLedgerEntriesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new ListStockLedgerEntriesQuery(item.Id, warehouse.Id, null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].ReferenceId.Should().Be("ref-1");
        result.Value![1].ReferenceId.Should().Be("ref-2");
    }

    [Fact]
    public async Task HandleAsync_Should_Filter_By_Batch_When_Given()
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
        db.StockLedgers.Add(new StockLedger
        {
            ItemId = item.Id, BranchId = branch.Id, WarehouseId = warehouse.Id, BatchNo = "B1", TransactionType = "GOODS_RECEIPT",
            ReferenceTable = "GoodsReceiptPoLine", ReferenceId = "ref-b1", QtyIn = 5, QtyOut = 0, UnitCost = 10,
            RunningBalanceQty = 5, RunningBalanceValue = 50, IsReversal = false,
            TransactionDate = DateTimeOffset.UtcNow, PostedAt = DateTimeOffset.UtcNow
        });
        db.StockLedgers.Add(new StockLedger
        {
            ItemId = item.Id, BranchId = branch.Id, WarehouseId = warehouse.Id, BatchNo = null, TransactionType = "GOODS_RECEIPT",
            ReferenceTable = "GoodsReceiptPoLine", ReferenceId = "ref-noBatch", QtyIn = 5, QtyOut = 0, UnitCost = 10,
            RunningBalanceQty = 5, RunningBalanceValue = 50, IsReversal = false,
            TransactionDate = DateTimeOffset.UtcNow, PostedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ListStockLedgerEntriesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new ListStockLedgerEntriesQuery(item.Id, warehouse.Id, "B1"), TestContext.Current.CancellationToken);

        result.Value.Should().ContainSingle(e => e.ReferenceId == "ref-b1");
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Empty_When_No_Entries_Match()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new ListStockLedgerEntriesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new ListStockLedgerEntriesQuery(Guid.NewGuid(), Guid.NewGuid(), null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
