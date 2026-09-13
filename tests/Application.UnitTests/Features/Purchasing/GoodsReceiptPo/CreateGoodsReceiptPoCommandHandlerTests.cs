namespace ZARI.Application.UnitTests.Features.Purchasing.GoodsReceiptPo;

using ZARI.Application.Features.Purchasing.GoodsReceiptPos.Create;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateGoodsReceiptPoCommandHandlerTests
{
    private static CreateGoodsReceiptPoCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new GetNextDocumentNumberCommandHandler(db), new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static CreateGoodsReceiptPoCommand Command(string branchId, Guid warehouseId, Guid supplierId, Guid itemId, Guid uomId, Guid? purchaseOrderId = null, Guid? purchaseOrderLineId = null, decimal qty = 5, Guid? locationId = null, Guid? costCenterId = null) =>
        new(branchId, warehouseId, supplierId, purchaseOrderId, "SINV-0001", DateTimeOffset.UtcNow, "delivered", costCenterId, "encoder",
            [new GoodsReceiptPoLineInput(itemId, null, null, qty, uomId, 50, locationId, purchaseOrderLineId)]);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, string branchId, Guid warehouseId, Guid supplierId, Guid itemId, Guid uomId)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        var supplier = PurchasingTestFixtures.Supplier();
        db.Suppliers.Add(supplier);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch.Id, warehouse.Id, supplier.Id, item.Id, uom.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Draft_Receipt()
    {
        var (db, branchId, warehouseId, supplierId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, supplierId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
        result.Value!.Lines.Should().ContainSingle();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branchId, warehouseId, supplierId, itemId, uomId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("GOODS_RECEIPT_PO", FormAction.Create, branchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(Command(branchId, warehouseId, supplierId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        var (db, _, warehouseId, supplierId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command("br-missing", warehouseId, supplierId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Warehouse_Not_Found()
    {
        var (db, branchId, _, supplierId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, Guid.NewGuid(), supplierId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Warehouse.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Supplier_Not_Found()
    {
        var (db, branchId, warehouseId, _, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, Guid.NewGuid(), itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Supplier.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Item_Not_Found()
    {
        var (db, branchId, warehouseId, supplierId, _, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, supplierId, Guid.NewGuid(), uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Item.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Uom_Not_Found()
    {
        var (db, branchId, warehouseId, supplierId, itemId, _) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, supplierId, itemId, Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Uom.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Storage_Location_Not_Found()
    {
        var (db, branchId, warehouseId, supplierId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, supplierId, itemId, uomId, locationId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("StorageLocation.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Cost_Center_Not_Found()
    {
        var (db, branchId, warehouseId, supplierId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, supplierId, itemId, uomId, costCenterId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CostCenter.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Purchase_Order_Not_Found()
    {
        var (db, branchId, warehouseId, supplierId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, supplierId, itemId, uomId, purchaseOrderId: Guid.NewGuid(), purchaseOrderLineId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PurchaseOrder.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Purchase_Order_Not_Posted()
    {
        var (db, branchId, warehouseId, supplierId, itemId, uomId) = await Seed();
        var po = PurchasingTestFixtures.PurchaseOrder(branchId, supplierId, itemId, uomId, status: "DRAFT");
        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, supplierId, itemId, uomId, purchaseOrderId: po.Id, purchaseOrderLineId: po.Lines[0].Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReceiptPo.PurchaseOrderNotPosted");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Line_Missing_Purchase_Order_Line()
    {
        var (db, branchId, warehouseId, supplierId, itemId, uomId) = await Seed();
        var po = PurchasingTestFixtures.PurchaseOrder(branchId, supplierId, itemId, uomId, status: "POSTED");
        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, supplierId, itemId, uomId, purchaseOrderId: po.Id, purchaseOrderLineId: null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReceiptPo.LineMissingPurchaseOrderLine");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Unexpected_Purchase_Order_Line()
    {
        var (db, branchId, warehouseId, supplierId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, supplierId, itemId, uomId, purchaseOrderId: null, purchaseOrderLineId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReceiptPo.UnexpectedPurchaseOrderLine");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Exceeds_Ordered_Qty()
    {
        var (db, branchId, warehouseId, supplierId, itemId, uomId) = await Seed();
        var po = PurchasingTestFixtures.PurchaseOrder(branchId, supplierId, itemId, uomId, status: "POSTED", qty: 10);
        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, supplierId, itemId, uomId, purchaseOrderId: po.Id, purchaseOrderLineId: po.Lines[0].Id, qty: 20), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReceiptPo.ExceedsOrderedQty");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Receipt_Against_Posted_Purchase_Order()
    {
        var (db, branchId, warehouseId, supplierId, itemId, uomId) = await Seed();
        var po = PurchasingTestFixtures.PurchaseOrder(branchId, supplierId, itemId, uomId, status: "POSTED", qty: 10);
        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, supplierId, itemId, uomId, purchaseOrderId: po.Id, purchaseOrderLineId: po.Lines[0].Id, qty: 5), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PurchaseOrderId.Should().Be(po.Id);
        await db.DisposeAsync();
    }
}
