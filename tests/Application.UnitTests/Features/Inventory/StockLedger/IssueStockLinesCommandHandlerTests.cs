namespace ZARI.Application.UnitTests.Features.Inventory.StockLedger;

using ZARI.Application.Features.Inventory.StockLedgers.Issue;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Entities;

/// <summary>
/// Same InMemory transaction blocker as ReceiveStockCommandHandlerTests — only the guard clauses
/// that run before `BeginTransactionAsync` are testable: an empty line list, a line list where
/// every item resolves to non-stocked (or an unknown item id), and the idempotency short-circuit
/// (every line's reference already posted). The actual insufficient-stock/available-stock
/// validations and the real balance mutation all live inside the transaction delegate and can't be
/// exercised here.
/// </summary>
public sealed class IssueStockLinesCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Succeed_With_Empty_Costs_When_No_Lines_Given()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new IssueStockLinesCommandHandler(db);

        var result = await handler.HandleAsync(new IssueStockLinesCommand([]), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CostsByReferenceId.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Succeed_With_Empty_Costs_When_Only_NonStocked_Items()
    {
        await using var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id, isStocked: false);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new IssueStockLinesCommandHandler(db);
        var line = new IssueStockLineItem(item.Id, "br-1", Guid.NewGuid(), null, 5, "SalesInvoiceLine", "ref-1", DateTimeOffset.UtcNow, null);

        var result = await handler.HandleAsync(new IssueStockLinesCommand([line]), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CostsByReferenceId.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Cached_Costs_When_Every_Line_Already_Posted()
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
            ItemId = item.Id, BranchId = branch.Id, WarehouseId = warehouse.Id, TransactionType = "GOODS_ISSUE",
            ReferenceTable = "SalesInvoiceLine", ReferenceId = "ref-1", QtyIn = 0, QtyOut = 5, UnitCost = 42,
            RunningBalanceQty = 0, RunningBalanceValue = 0, IsReversal = false,
            TransactionDate = DateTimeOffset.UtcNow, PostedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new IssueStockLinesCommandHandler(db);
        var line = new IssueStockLineItem(item.Id, branch.Id, warehouse.Id, null, 5, "SalesInvoiceLine", "ref-1", DateTimeOffset.UtcNow, null);

        var result = await handler.HandleAsync(new IssueStockLinesCommand([line]), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CostsByReferenceId.Should().ContainKey("ref-1").WhoseValue.Should().Be(42);
        db.StockLedgers.Should().ContainSingle();
    }
}
