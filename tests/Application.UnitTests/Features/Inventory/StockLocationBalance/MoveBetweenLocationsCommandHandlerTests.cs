namespace ZARI.Application.UnitTests.Features.Inventory.StockLocationBalance;

using ZARI.Application.Features.Inventory.StockLocationBalances.Move;
using ZARI.Application.UnitTests.TestSupport;

/// <summary>Only the two guard clauses before `BeginTransactionAsync` are testable — the InsufficientQty check and the actual move both live inside the transaction delegate.</summary>
public sealed class MoveBetweenLocationsCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_Item_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new MoveBetweenLocationsCommandHandler(db);

        var result = await handler.HandleAsync(new MoveBetweenLocationsCommand(Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), Guid.NewGuid(), 5), TestContext.Current.CancellationToken);

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
        var handler = new MoveBetweenLocationsCommandHandler(db);

        var result = await handler.HandleAsync(new MoveBetweenLocationsCommand(item.Id, Guid.NewGuid(), null, Guid.NewGuid(), Guid.NewGuid(), 5), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Warehouse.NotFound");
    }
}
