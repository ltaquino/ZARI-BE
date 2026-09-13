namespace ZARI.Application.UnitTests.Features.Inventory.StockAdjustment;

using ZARI.Application.Features.Inventory.StockAdjustments.Delete;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class DeleteStockAdjustmentCommandHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.StockAdjustment adjustment, ZARI.Domain.Entities.Branch branch)> Seed(string status = "DRAFT")
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
        return (db, adjustment, branch);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteStockAdjustmentCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteStockAdjustmentCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, adjustment, branch) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("STOCK_ADJUSTMENTS", FormAction.Delete, branch.Id, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteStockAdjustmentCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeleteStockAdjustmentCommand(adjustment.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, adjustment, _) = await Seed(status: "POSTED");
        var handler = new DeleteStockAdjustmentCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteStockAdjustmentCommand(adjustment.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("StockAdjustment.NotDraft");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Delete_Draft_Adjustment()
    {
        var (db, adjustment, _) = await Seed();
        var handler = new DeleteStockAdjustmentCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteStockAdjustmentCommand(adjustment.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.StockAdjustments.Should().BeEmpty();
        await db.DisposeAsync();
    }
}
