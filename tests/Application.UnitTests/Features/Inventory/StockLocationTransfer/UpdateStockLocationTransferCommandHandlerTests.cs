namespace ZARI.Application.UnitTests.Features.Inventory.StockLocationTransfer;

using ZARI.Application.Features.Inventory.StockLocationTransfers.Create;
using ZARI.Application.Features.Inventory.StockLocationTransfers.Update;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class UpdateStockLocationTransferCommandHandlerTests
{
    private static UpdateStockLocationTransferCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static UpdateStockLocationTransferCommand Command(Guid id, string branchId, Guid warehouseId, Guid itemId, Guid fromLocationId, Guid toLocationId) =>
        new(id, branchId, warehouseId, DateTimeOffset.UtcNow, "remarks", "updater",
            [new StockLocationTransferLineInput(itemId, "BATCH-1", null, fromLocationId, toLocationId, 3)]);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.StockLocationTransfer transfer, ZARI.Domain.Entities.Branch branch, ZARI.Domain.Entities.Warehouse warehouse, ZARI.Domain.Entities.Item item, ZARI.Domain.Entities.StorageLocation fromLocation, ZARI.Domain.Entities.StorageLocation toLocation)> Seed(string status = "DRAFT")
    {
        var db = TestDbContextFactory.Create();
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
        var transfer = InventoryTestFixtures.StockLocationTransfer(branch.Id, warehouse.Id, item.Id, fromLocation.Id, toLocation.Id, status: status);
        db.StockLocationTransfers.Add(transfer);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, transfer, branch, warehouse, item, fromLocation, toLocation);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(Command(Guid.NewGuid(), "br-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, transfer, branch, warehouse, item, fromLocation, toLocation) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("STOCK_LOCATION_TRANSFERS", FormAction.Edit, branch.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(Command(transfer.Id, branch.Id, warehouse.Id, item.Id, fromLocation.Id, toLocation.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, transfer, branch, warehouse, item, fromLocation, toLocation) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(Command(transfer.Id, branch.Id, warehouse.Id, item.Id, fromLocation.Id, toLocation.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("StockLocationTransfer.NotDraft");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Warehouse_Not_Found()
    {
        var (db, transfer, branch, _, item, fromLocation, toLocation) = await Seed();

        var result = await Handler(db).HandleAsync(Command(transfer.Id, branch.Id, Guid.NewGuid(), item.Id, fromLocation.Id, toLocation.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Warehouse.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        var (db, transfer, _, warehouse, item, fromLocation, toLocation) = await Seed();

        var result = await Handler(db).HandleAsync(Command(transfer.Id, "br-missing", warehouse.Id, item.Id, fromLocation.Id, toLocation.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Item_Not_Found()
    {
        var (db, transfer, branch, warehouse, _, fromLocation, toLocation) = await Seed();

        var result = await Handler(db).HandleAsync(Command(transfer.Id, branch.Id, warehouse.Id, Guid.NewGuid(), fromLocation.Id, toLocation.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Item.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Storage_Location_Not_Found()
    {
        var (db, transfer, branch, warehouse, item, fromLocation, _) = await Seed();

        var result = await Handler(db).HandleAsync(Command(transfer.Id, branch.Id, warehouse.Id, item.Id, fromLocation.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("StorageLocation.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Update_Draft_Transfer()
    {
        var (db, transfer, branch, warehouse, item, fromLocation, toLocation) = await Seed();

        var result = await Handler(db).HandleAsync(Command(transfer.Id, branch.Id, warehouse.Id, item.Id, fromLocation.Id, toLocation.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Lines.Should().ContainSingle(l => l.Qty == 3);
        await db.DisposeAsync();
    }
}
