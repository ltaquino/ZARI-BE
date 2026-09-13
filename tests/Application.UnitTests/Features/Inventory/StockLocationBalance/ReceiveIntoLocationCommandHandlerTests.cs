namespace ZARI.Application.UnitTests.Features.Inventory.StockLocationBalance;

using ZARI.Application.Features.Inventory.StockLocationBalances.Receive;
using ZARI.Application.UnitTests.TestSupport;

/// <summary>
/// Only the guard clauses before `BeginTransactionAsync` are testable — same InMemory transaction
/// blocker as StockLedger's Receive/Issue/Reverse. The actual balance mutation is untestable here.
/// </summary>
public sealed class ReceiveIntoLocationCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_Item_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new ReceiveIntoLocationCommandHandler(db);

        var result = await handler.HandleAsync(new ReceiveIntoLocationCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 5), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Item.NotFound");
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
        var handler = new ReceiveIntoLocationCommandHandler(db);

        var result = await handler.HandleAsync(new ReceiveIntoLocationCommand(item.Id, Guid.NewGuid(), Guid.NewGuid(), null, 5), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Warehouse.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Storage_Location_Not_Found()
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
        var handler = new ReceiveIntoLocationCommandHandler(db);

        var result = await handler.HandleAsync(new ReceiveIntoLocationCommand(item.Id, warehouse.Id, Guid.NewGuid(), null, 5), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("StorageLocation.NotFound");
    }
}
