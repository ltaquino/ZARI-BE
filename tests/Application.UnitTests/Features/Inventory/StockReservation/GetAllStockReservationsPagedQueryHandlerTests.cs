namespace ZARI.Application.UnitTests.Features.Inventory.StockReservation;

using ZARI.Application.Features.Inventory.StockReservations.GetAllPaged;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetAllStockReservationsPagedQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("STOCK_RESERVATIONS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllStockReservationsPagedQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllStockReservationsPagedQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Filter_By_Search_And_Page()
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
        var match = new StockReservation { ItemId = item.Id, BranchId = branch.Id, WarehouseId = warehouse.Id, QtyReserved = 5, ReservedDate = DateTimeOffset.UtcNow, Status = "ACTIVE", ReferenceNote = "VIP-MATCH" };
        var noMatch = new StockReservation { ItemId = item.Id, BranchId = branch.Id, WarehouseId = warehouse.Id, QtyReserved = 3, ReservedDate = DateTimeOffset.UtcNow, Status = "ACTIVE", ReferenceNote = "regular" };
        db.StockReservations.AddRange(match, noMatch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllStockReservationsPagedQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllStockReservationsPagedQuery(1, 20, "MATCH"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle(r => r.Id == match.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Empty_Page_When_No_Reservations_Exist()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetAllStockReservationsPagedQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllStockReservationsPagedQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }
}
