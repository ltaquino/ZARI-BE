namespace ZARI.Application.UnitTests.Features.Inventory.StockReservation;

using ZARI.Application.Features.Inventory.StockReservations.Release;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class ReleaseStockReservationCommandHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, StockReservation reservation, Branch branch)> Seed(string status = "ACTIVE")
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
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var reservation = new StockReservation
        {
            ItemId = item.Id, BranchId = branch.Id, WarehouseId = warehouse.Id, QtyReserved = 10,
            ReservedDate = DateTimeOffset.UtcNow, Status = status
        };
        db.StockReservations.Add(reservation);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, reservation, branch);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new ReleaseStockReservationCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new ReleaseStockReservationCommand(Guid.NewGuid(), "manager"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, reservation, branch) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("STOCK_RESERVATIONS", FormAction.Edit, branch.Id, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new ReleaseStockReservationCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new ReleaseStockReservationCommand(reservation.Id, "manager"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Active()
    {
        var (db, reservation, _) = await Seed(status: "RELEASED");
        var handler = new ReleaseStockReservationCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new ReleaseStockReservationCommand(reservation.Id, "manager"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("StockReservation.NotActive");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Release_Active_Reservation()
    {
        var (db, reservation, _) = await Seed();
        var handler = new ReleaseStockReservationCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new ReleaseStockReservationCommand(reservation.Id, "manager"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.StockReservations.FindAsync([reservation.Id], TestContext.Current.CancellationToken);
        updated!.Status.Should().Be("RELEASED");
        updated.ReleasedBy.Should().Be("manager");
        await db.DisposeAsync();
    }
}
