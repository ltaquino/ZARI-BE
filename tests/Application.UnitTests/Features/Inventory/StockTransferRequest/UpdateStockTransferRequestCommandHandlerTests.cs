namespace ZARI.Application.UnitTests.Features.Inventory.StockTransferRequest;

using ZARI.Application.Features.Inventory.StockTransferRequests.Create;
using ZARI.Application.Features.Inventory.StockTransferRequests.Update;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class UpdateStockTransferRequestCommandHandlerTests
{
    private static UpdateStockTransferRequestCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static UpdateStockTransferRequestCommand Command(Guid id, string sourceBranchId, Guid sourceWarehouseId, string destBranchId, Guid destWarehouseId, Guid itemId, Guid uomId) =>
        new(id, sourceBranchId, sourceWarehouseId, destBranchId, destWarehouseId, DateTimeOffset.UtcNow, "remarks", "updater",
            [new StockTransferRequestLineInput(itemId, 6, uomId)]);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.StockTransferRequest request, ZARI.Domain.Entities.Branch sourceBranch, ZARI.Domain.Entities.Warehouse sourceWarehouse, ZARI.Domain.Entities.Branch destBranch, ZARI.Domain.Entities.Warehouse destWarehouse, ZARI.Domain.Entities.Item item, ZARI.Domain.Entities.Uom uom)> Seed(string status = "DRAFT")
    {
        var db = TestDbContextFactory.Create();
        var sourceBranch = LoanTestFixtures.Branch("br-1");
        db.Branches.Add(sourceBranch);
        var destBranch = LoanTestFixtures.Branch("br-2");
        db.Branches.Add(destBranch);
        var sourceWarehouse = InventoryTestFixtures.Warehouse(sourceBranch.Id);
        db.Warehouses.Add(sourceWarehouse);
        var destWarehouse = InventoryTestFixtures.Warehouse(destBranch.Id, code: "WH2");
        db.Warehouses.Add(destWarehouse);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var request = InventoryTestFixtures.StockTransferRequest(sourceBranch.Id, sourceWarehouse.Id, destBranch.Id, destWarehouse.Id, item.Id, uom.Id, status: status);
        db.StockTransferRequests.Add(request);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, request, sourceBranch, sourceWarehouse, destBranch, destWarehouse, item, uom);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(Command(Guid.NewGuid(), "br-1", Guid.NewGuid(), "br-2", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, request, sourceBranch, sourceWarehouse, destBranch, destWarehouse, item, uom) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("STOCK_TRANSFER_REQUESTS", FormAction.Edit, destBranch.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(Command(request.Id, sourceBranch.Id, sourceWarehouse.Id, destBranch.Id, destWarehouse.Id, item.Id, uom.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, request, sourceBranch, sourceWarehouse, destBranch, destWarehouse, item, uom) = await Seed(status: "APPROVED");

        var result = await Handler(db).HandleAsync(Command(request.Id, sourceBranch.Id, sourceWarehouse.Id, destBranch.Id, destWarehouse.Id, item.Id, uom.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("StockTransferRequest.NotDraft");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Source_Warehouse_Not_Found()
    {
        var (db, request, sourceBranch, _, destBranch, destWarehouse, item, uom) = await Seed();

        var result = await Handler(db).HandleAsync(Command(request.Id, sourceBranch.Id, Guid.NewGuid(), destBranch.Id, destWarehouse.Id, item.Id, uom.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Warehouse.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Dest_Warehouse_Not_Found()
    {
        var (db, request, sourceBranch, sourceWarehouse, destBranch, _, item, uom) = await Seed();

        var result = await Handler(db).HandleAsync(Command(request.Id, sourceBranch.Id, sourceWarehouse.Id, destBranch.Id, Guid.NewGuid(), item.Id, uom.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Warehouse.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Source_Branch_Not_Found()
    {
        var (db, request, _, sourceWarehouse, destBranch, destWarehouse, item, uom) = await Seed();

        var result = await Handler(db).HandleAsync(Command(request.Id, "br-missing", sourceWarehouse.Id, destBranch.Id, destWarehouse.Id, item.Id, uom.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Dest_Branch_Not_Found()
    {
        var (db, request, sourceBranch, sourceWarehouse, _, destWarehouse, item, uom) = await Seed();

        var result = await Handler(db).HandleAsync(Command(request.Id, sourceBranch.Id, sourceWarehouse.Id, "br-missing", destWarehouse.Id, item.Id, uom.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Item_Not_Found()
    {
        var (db, request, sourceBranch, sourceWarehouse, destBranch, destWarehouse, _, uom) = await Seed();

        var result = await Handler(db).HandleAsync(Command(request.Id, sourceBranch.Id, sourceWarehouse.Id, destBranch.Id, destWarehouse.Id, Guid.NewGuid(), uom.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Item.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Uom_Not_Found()
    {
        var (db, request, sourceBranch, sourceWarehouse, destBranch, destWarehouse, item, _) = await Seed();

        var result = await Handler(db).HandleAsync(Command(request.Id, sourceBranch.Id, sourceWarehouse.Id, destBranch.Id, destWarehouse.Id, item.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Uom.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Update_Draft_Request()
    {
        var (db, request, sourceBranch, sourceWarehouse, destBranch, destWarehouse, item, uom) = await Seed();

        var result = await Handler(db).HandleAsync(Command(request.Id, sourceBranch.Id, sourceWarehouse.Id, destBranch.Id, destWarehouse.Id, item.Id, uom.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Lines.Should().ContainSingle(l => l.QtyRequested == 6);
        await db.DisposeAsync();
    }
}
