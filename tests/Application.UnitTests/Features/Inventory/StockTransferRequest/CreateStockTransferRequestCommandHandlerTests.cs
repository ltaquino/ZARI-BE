namespace ZARI.Application.UnitTests.Features.Inventory.StockTransferRequest;

using ZARI.Application.Features.Inventory.StockTransferRequests.Create;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateStockTransferRequestCommandHandlerTests
{
    private static CreateStockTransferRequestCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db,
            LoanTestFixtures.SuccessHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>>(Result.Success(new NextDocumentNumberResponse("STR-000001"))),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static CreateStockTransferRequestCommand Command(string sourceBranchId, Guid sourceWarehouseId, string destBranchId, Guid destWarehouseId, Guid itemId, Guid uomId) =>
        new(sourceBranchId, sourceWarehouseId, destBranchId, destWarehouseId, DateTimeOffset.UtcNow, "remarks", "creator",
            [new StockTransferRequestLineInput(itemId, 10, uomId)]);

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("STOCK_TRANSFER_REQUESTS", FormAction.Create, "br-2", Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(Command("br-1", Guid.NewGuid(), "br-2", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Source_Warehouse_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await Handler(db).HandleAsync(Command("br-1", Guid.NewGuid(), "br-2", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Warehouse.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Dest_Warehouse_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var sourceBranch = LoanTestFixtures.Branch("br-1");
        db.Branches.Add(sourceBranch);
        var sourceWarehouse = InventoryTestFixtures.Warehouse(sourceBranch.Id);
        db.Warehouses.Add(sourceWarehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(sourceBranch.Id, sourceWarehouse.Id, "br-2", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Warehouse.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Source_Branch_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var sourceBranch = LoanTestFixtures.Branch("br-1");
        db.Branches.Add(sourceBranch);
        var destBranch = LoanTestFixtures.Branch("br-2");
        db.Branches.Add(destBranch);
        var sourceWarehouse = InventoryTestFixtures.Warehouse(sourceBranch.Id);
        db.Warehouses.Add(sourceWarehouse);
        var destWarehouse = InventoryTestFixtures.Warehouse(destBranch.Id, code: "WH2");
        db.Warehouses.Add(destWarehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command("br-missing", sourceWarehouse.Id, destBranch.Id, destWarehouse.Id, Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Dest_Branch_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var sourceBranch = LoanTestFixtures.Branch("br-1");
        db.Branches.Add(sourceBranch);
        var sourceWarehouse = InventoryTestFixtures.Warehouse(sourceBranch.Id);
        db.Warehouses.Add(sourceWarehouse);
        var destWarehouse = InventoryTestFixtures.Warehouse(sourceBranch.Id, code: "WH2");
        db.Warehouses.Add(destWarehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(sourceBranch.Id, sourceWarehouse.Id, "br-missing", destWarehouse.Id, Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Item_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var sourceBranch = LoanTestFixtures.Branch("br-1");
        db.Branches.Add(sourceBranch);
        var destBranch = LoanTestFixtures.Branch("br-2");
        db.Branches.Add(destBranch);
        var sourceWarehouse = InventoryTestFixtures.Warehouse(sourceBranch.Id);
        db.Warehouses.Add(sourceWarehouse);
        var destWarehouse = InventoryTestFixtures.Warehouse(destBranch.Id, code: "WH2");
        db.Warehouses.Add(destWarehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(sourceBranch.Id, sourceWarehouse.Id, destBranch.Id, destWarehouse.Id, Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Item.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Uom_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
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

        var result = await Handler(db).HandleAsync(Command(sourceBranch.Id, sourceWarehouse.Id, destBranch.Id, destWarehouse.Id, item.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Uom.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Request()
    {
        await using var db = TestDbContextFactory.Create();
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

        var result = await Handler(db).HandleAsync(Command(sourceBranch.Id, sourceWarehouse.Id, destBranch.Id, destWarehouse.Id, item.Id, uom.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
        result.Value.Lines.Should().ContainSingle();
    }
}
