namespace ZARI.Application.UnitTests.Features.Inventory.GoodsIssue;

using ZARI.Application.Features.Inventory.GoodsIssues.Create;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateGoodsIssueCommandHandlerTests
{
    private static CreateGoodsIssueCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db,
            LoanTestFixtures.SuccessHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>>(Result.Success(new NextDocumentNumberResponse("GI-000001"))),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static CreateGoodsIssueCommand Command(string branchId, Guid warehouseId, Guid itemId, Guid uomId, string referenceType = "INTERNAL_USE", string? reasonCode = "DAMAGE", string? destBranchId = null, Guid? destWarehouseId = null, Guid? costCenterId = null) =>
        new(branchId, warehouseId, referenceType, destBranchId, destWarehouseId, reasonCode, DateTimeOffset.UtcNow, "remarks", null, null, costCenterId, "creator",
            [new GoodsIssueLineInput(itemId, "BATCH-1", null, 10, uomId, 50)]);

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("GOODS_ISSUES", FormAction.Create, "br-1", Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(Command("br-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Warehouse_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await Handler(db).HandleAsync(Command("br-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Warehouse.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Dest_Warehouse_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var destBranch = LoanTestFixtures.Branch("br-2");
        db.Branches.Add(destBranch);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branch.Id, warehouse.Id, Guid.NewGuid(), Guid.NewGuid(), referenceType: "STOCK_TRANSFER", reasonCode: null, destBranchId: destBranch.Id, destWarehouseId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Warehouse.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command("br-missing", warehouse.Id, Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Dest_Branch_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        var destWarehouse = InventoryTestFixtures.Warehouse(branch.Id, code: "WH2");
        db.Warehouses.Add(destWarehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branch.Id, warehouse.Id, Guid.NewGuid(), Guid.NewGuid(), referenceType: "STOCK_TRANSFER", reasonCode: null, destBranchId: "br-missing", destWarehouseId: destWarehouse.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Item_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branch.Id, warehouse.Id, Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Item.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Uom_Not_Found()
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

        var result = await Handler(db).HandleAsync(Command(branch.Id, warehouse.Id, item.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Uom.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Cost_Center_Not_Found()
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

        var result = await Handler(db).HandleAsync(Command(branch.Id, warehouse.Id, item.Id, uom.Id, costCenterId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CostCenter.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Internal_Use_Issue_And_Discard_Dest()
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

        var result = await Handler(db).HandleAsync(Command(branch.Id, warehouse.Id, item.Id, uom.Id, referenceType: "INTERNAL_USE", reasonCode: "DAMAGE"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
        result.Value.ReferenceType.Should().Be("INTERNAL_USE");
        result.Value.DestBranchId.Should().BeNull();
        result.Value.ReasonCode.Should().Be("DAMAGE");
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Stock_Transfer_Issue_And_Discard_Reason()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var destBranch = LoanTestFixtures.Branch("br-2");
        db.Branches.Add(destBranch);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        var destWarehouse = InventoryTestFixtures.Warehouse(destBranch.Id, code: "WH2");
        db.Warehouses.Add(destWarehouse);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branch.Id, warehouse.Id, item.Id, uom.Id, referenceType: "STOCK_TRANSFER", reasonCode: "ignored", destBranchId: destBranch.Id, destWarehouseId: destWarehouse.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ReferenceType.Should().Be("STOCK_TRANSFER");
        result.Value.DestBranchId.Should().Be(destBranch.Id);
        result.Value.DestWarehouseId.Should().Be(destWarehouse.Id);
        result.Value.ReasonCode.Should().BeNull();
    }
}
