namespace ZARI.Application.UnitTests.Features.Inventory.StockLocationTransfer;

using ZARI.Application.Features.Inventory.StockLocationTransfers.Delete;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class DeleteStockLocationTransferCommandHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.StockLocationTransfer transfer, ZARI.Domain.Entities.Branch branch)> Seed(string status = "DRAFT")
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
        return (db, transfer, branch);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteStockLocationTransferCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteStockLocationTransferCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, transfer, branch) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("STOCK_LOCATION_TRANSFERS", FormAction.Delete, branch.Id, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteStockLocationTransferCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeleteStockLocationTransferCommand(transfer.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, transfer, _) = await Seed(status: "POSTED");
        var handler = new DeleteStockLocationTransferCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteStockLocationTransferCommand(transfer.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("StockLocationTransfer.NotDraft");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Delete_Draft_Transfer()
    {
        var (db, transfer, _) = await Seed();
        var handler = new DeleteStockLocationTransferCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteStockLocationTransferCommand(transfer.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.StockLocationTransfers.Should().BeEmpty();
        await db.DisposeAsync();
    }
}
