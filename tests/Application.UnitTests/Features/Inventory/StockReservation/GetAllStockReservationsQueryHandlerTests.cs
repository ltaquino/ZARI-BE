namespace ZARI.Application.UnitTests.Features.Inventory.StockReservation;

using ZARI.Application.Features.Inventory.StockReservations.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetAllStockReservationsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("STOCK_RESERVATIONS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllStockReservationsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllStockReservationsQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Reservations_Ordered_By_Date_Descending()
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
        var older = new StockReservation { ItemId = item.Id, BranchId = branch.Id, WarehouseId = warehouse.Id, QtyReserved = 5, ReservedDate = DateTimeOffset.UtcNow.AddDays(-5), Status = "ACTIVE" };
        var newer = new StockReservation { ItemId = item.Id, BranchId = branch.Id, WarehouseId = warehouse.Id, QtyReserved = 3, ReservedDate = DateTimeOffset.UtcNow, Status = "ACTIVE" };
        db.StockReservations.AddRange(older, newer);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllStockReservationsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllStockReservationsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].Id.Should().Be(newer.Id);
    }
}
