namespace ZARI.Application.UnitTests.Features.Inventory.StockLedger;

using ZARI.Application.Features.Inventory.StockLedgers.Receive;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Only the guard clauses that run BEFORE `dbContext.Database.BeginTransactionAsync` are
/// exercised here — the InMemory provider rejects that call outright (confirmed directly in an
/// earlier phase of this sweep), so the actual balance-mutation path (a first-time receive) can't
/// be unit-tested in this stack. Item.NotFound, the non-stocked no-op, Warehouse.NotFound, and the
/// idempotency short-circuit (a retry of an already-posted reference) all happen before that point
/// and are fully covered.
/// </summary>
public sealed class ReceiveStockCommandHandlerTests
{
    private static ReceiveStockCommand Command(Guid itemId, Guid warehouseId, string referenceId = "ref-1") =>
        new(itemId, "br-1", warehouseId, null, 10, 50, "GoodsReceiptPoLine", referenceId, DateTimeOffset.UtcNow, null);

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Item_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new ReceiveStockCommandHandler(db);

        var result = await handler.HandleAsync(Command(Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Item.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_NoOp_For_A_NonStocked_Item()
    {
        await using var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id, isStocked: false);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ReceiveStockCommandHandler(db);

        var result = await handler.HandleAsync(Command(item.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UnitCost.Should().BeNull();
        db.StockLedgers.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Warehouse_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ReceiveStockCommandHandler(db);

        var result = await handler.HandleAsync(Command(item.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Warehouse.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Cached_Cost_When_Reference_Already_Posted()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.StockLedgers.Add(new StockLedger
        {
            ItemId = item.Id, BranchId = branch.Id, WarehouseId = warehouse.Id, TransactionType = "GOODS_RECEIPT",
            ReferenceTable = "GoodsReceiptPoLine", ReferenceId = "ref-1", QtyIn = 10, QtyOut = 0, UnitCost = 75,
            RunningBalanceQty = 10, RunningBalanceValue = 750, IsReversal = false,
            TransactionDate = DateTimeOffset.UtcNow, PostedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ReceiveStockCommandHandler(db);

        var result = await handler.HandleAsync(Command(item.Id, warehouse.Id, referenceId: "ref-1"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UnitCost.Should().Be(75);
        db.StockLedgers.Should().ContainSingle();
    }
}
