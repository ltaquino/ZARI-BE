namespace ZARI.Application.UnitTests.Features.Inventory.StockLocationTransfer;

using ZARI.Application.Features.Inventory.StockLocationTransfers.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetAllStockLocationTransfersQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("STOCK_LOCATION_TRANSFERS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllStockLocationTransfersQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllStockLocationTransfersQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Transfers_Ordered_By_Date_Descending()
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
        var fromLocation = InventoryTestFixtures.StorageLocation(warehouse.Id);
        db.StorageLocations.Add(fromLocation);
        var toLocation = InventoryTestFixtures.StorageLocation(warehouse.Id, binCode: "02");
        db.StorageLocations.Add(toLocation);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var older = InventoryTestFixtures.StockLocationTransfer(branch.Id, warehouse.Id, item.Id, fromLocation.Id, toLocation.Id);
        older.TransferDate = DateTimeOffset.UtcNow.AddDays(-5);
        var newer = InventoryTestFixtures.StockLocationTransfer(branch.Id, warehouse.Id, item.Id, fromLocation.Id, toLocation.Id);
        newer.TransferDate = DateTimeOffset.UtcNow;
        db.StockLocationTransfers.AddRange(older, newer);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllStockLocationTransfersQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllStockLocationTransfersQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].Id.Should().Be(newer.Id);
    }
}
