namespace ZARI.Application.UnitTests.Features.Purchasing.GoodsReturn;

using ZARI.Application.Features.Purchasing.GoodsReturns.Create;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateGoodsReturnCommandHandlerTests
{
    private static CreateGoodsReturnCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new GetNextDocumentNumberCommandHandler(db), new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static CreateGoodsReturnCommand Command(string branchId, Guid warehouseId, Guid supplierId, Guid itemId, Guid uomId, Guid? goodsReceiptPoId = null, Guid? goodsReceiptPoLineId = null, decimal qty = 3, string reasonCode = "DAMAGED") =>
        new(branchId, warehouseId, supplierId, goodsReceiptPoId, reasonCode, DateTimeOffset.UtcNow, "defective", null, "encoder",
            [new GoodsReturnLineInput(itemId, null, null, qty, uomId, 50, goodsReceiptPoLineId)]);

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
        db.PurchaseReturnReasons.Add(PurchasingTestFixtures.PurchaseReturnReason());
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch.Id, warehouse.Id, supplier.Id, item.Id, uom.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Draft_Return()
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
        permissions.HasPermissionOnBranchAsync("GOODS_RETURNS", FormAction.Create, branchId, Arg.Any<CancellationToken>()).Returns(false);

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
    public async Task HandleAsync_Should_Fail_When_Reason_Not_Found()
    {
        var (db, branchId, warehouseId, supplierId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, supplierId, itemId, uomId, reasonCode: "NOPE"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PurchaseReturnReason.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Goods_Receipt_Po_Not_Found()
    {
        var (db, branchId, warehouseId, supplierId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, supplierId, itemId, uomId, goodsReceiptPoId: Guid.NewGuid(), goodsReceiptPoLineId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReceiptPo.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Goods_Receipt_Po_Not_Posted()
    {
        var (db, branchId, warehouseId, supplierId, itemId, uomId) = await Seed();
        var grpo = PurchasingTestFixtures.GoodsReceiptPo(branchId, warehouseId, supplierId, itemId, uomId, status: "DRAFT");
        db.GoodsReceiptPos.Add(grpo);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, supplierId, itemId, uomId, goodsReceiptPoId: grpo.Id, goodsReceiptPoLineId: grpo.Lines[0].Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReturn.GoodsReceiptPoNotPosted");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Line_Missing_Goods_Receipt_Po_Line()
    {
        var (db, branchId, warehouseId, supplierId, itemId, uomId) = await Seed();
        var grpo = PurchasingTestFixtures.GoodsReceiptPo(branchId, warehouseId, supplierId, itemId, uomId, status: "POSTED");
        db.GoodsReceiptPos.Add(grpo);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, supplierId, itemId, uomId, goodsReceiptPoId: grpo.Id, goodsReceiptPoLineId: null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReturn.LineMissingGoodsReceiptPoLine");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Unexpected_Goods_Receipt_Po_Line()
    {
        var (db, branchId, warehouseId, supplierId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, supplierId, itemId, uomId, goodsReceiptPoId: null, goodsReceiptPoLineId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReturn.UnexpectedGoodsReceiptPoLine");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Exceeds_Received_Qty()
    {
        var (db, branchId, warehouseId, supplierId, itemId, uomId) = await Seed();
        var grpo = PurchasingTestFixtures.GoodsReceiptPo(branchId, warehouseId, supplierId, itemId, uomId, status: "POSTED", qty: 5);
        db.GoodsReceiptPos.Add(grpo);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, supplierId, itemId, uomId, goodsReceiptPoId: grpo.Id, goodsReceiptPoLineId: grpo.Lines[0].Id, qty: 10), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReturn.ExceedsReceivedQty");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Return_Against_Posted_Goods_Receipt_Po()
    {
        var (db, branchId, warehouseId, supplierId, itemId, uomId) = await Seed();
        var grpo = PurchasingTestFixtures.GoodsReceiptPo(branchId, warehouseId, supplierId, itemId, uomId, status: "POSTED", qty: 10);
        db.GoodsReceiptPos.Add(grpo);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, supplierId, itemId, uomId, goodsReceiptPoId: grpo.Id, goodsReceiptPoLineId: grpo.Lines[0].Id, qty: 4), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.GoodsReceiptPoId.Should().Be(grpo.Id);
        await db.DisposeAsync();
    }
}
