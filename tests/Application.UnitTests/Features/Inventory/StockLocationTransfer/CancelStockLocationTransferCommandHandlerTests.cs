namespace ZARI.Application.UnitTests.Features.Inventory.StockLocationTransfer;

using ZARI.Application.Features.Inventory.StockLocationTransfers.Cancel;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CancelStockLocationTransferCommandHandlerTests
{
    private static CancelStockLocationTransferCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

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
        var result = await Handler(db).HandleAsync(new CancelStockLocationTransferCommand(Guid.NewGuid(), "encoder", "mistake"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, transfer, branch) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("STOCK_LOCATION_TRANSFERS", FormAction.Cancel, branch.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new CancelStockLocationTransferCommand(transfer.Id, "encoder", "mistake"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, transfer, _) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(new CancelStockLocationTransferCommand(transfer.Id, "encoder", "mistake"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("StockLocationTransfer.NotDraft");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Cancel_Draft_Transfer()
    {
        var (db, transfer, _) = await Seed();

        var result = await Handler(db).HandleAsync(new CancelStockLocationTransferCommand(transfer.Id, "encoder", "mistake"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("CANCELLED");
        await db.DisposeAsync();
    }
}
