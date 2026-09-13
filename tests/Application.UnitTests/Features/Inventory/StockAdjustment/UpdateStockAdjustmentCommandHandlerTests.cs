namespace ZARI.Application.UnitTests.Features.Inventory.StockAdjustment;

using ZARI.Application.Features.Inventory.StockAdjustments.Create;
using ZARI.Application.Features.Inventory.StockAdjustments.Update;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class UpdateStockAdjustmentCommandHandlerTests
{
    private static UpdateStockAdjustmentCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static UpdateStockAdjustmentCommand Command(Guid id, string branchId, Guid warehouseId, Guid itemId, Guid? costCenterId = null) =>
        new(id, branchId, warehouseId, DateTimeOffset.UtcNow, "DAMAGE", "remarks", costCenterId, "updater",
            [new StockAdjustmentLineInput(itemId, "BATCH-1", null, 10, 5, 50)]);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.StockAdjustment adjustment, ZARI.Domain.Entities.Branch branch, ZARI.Domain.Entities.Warehouse warehouse, ZARI.Domain.Entities.Item item)> Seed(string status = "DRAFT")
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
        var adjustment = InventoryTestFixtures.StockAdjustment(branch.Id, warehouse.Id, item.Id, status: status);
        db.StockAdjustments.Add(adjustment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, adjustment, branch, warehouse, item);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(Command(Guid.NewGuid(), "br-1", Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, adjustment, branch, warehouse, item) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("STOCK_ADJUSTMENTS", FormAction.Edit, branch.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(Command(adjustment.Id, branch.Id, warehouse.Id, item.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, adjustment, branch, warehouse, item) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(Command(adjustment.Id, branch.Id, warehouse.Id, item.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("StockAdjustment.NotDraft");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Warehouse_Not_Found()
    {
        var (db, adjustment, branch, _, item) = await Seed();

        var result = await Handler(db).HandleAsync(Command(adjustment.Id, branch.Id, Guid.NewGuid(), item.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Warehouse.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        var (db, adjustment, _, warehouse, item) = await Seed();

        var result = await Handler(db).HandleAsync(Command(adjustment.Id, "br-missing", warehouse.Id, item.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Item_Not_Found()
    {
        var (db, adjustment, branch, warehouse, _) = await Seed();

        var result = await Handler(db).HandleAsync(Command(adjustment.Id, branch.Id, warehouse.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Item.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Cost_Center_Not_Found()
    {
        var (db, adjustment, branch, warehouse, item) = await Seed();

        var result = await Handler(db).HandleAsync(Command(adjustment.Id, branch.Id, warehouse.Id, item.Id, costCenterId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CostCenter.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Update_Draft_Adjustment()
    {
        var (db, adjustment, branch, warehouse, item) = await Seed();

        var result = await Handler(db).HandleAsync(Command(adjustment.Id, branch.Id, warehouse.Id, item.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Lines.Should().ContainSingle(l => l.VarianceQty == -5);
        await db.DisposeAsync();
    }
}
