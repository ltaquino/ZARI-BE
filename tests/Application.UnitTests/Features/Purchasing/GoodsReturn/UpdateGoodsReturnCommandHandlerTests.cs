namespace ZARI.Application.UnitTests.Features.Purchasing.GoodsReturn;

using ZARI.Application.Features.Purchasing.GoodsReturns.Create;
using ZARI.Application.Features.Purchasing.GoodsReturns.Update;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdateGoodsReturnCommandHandlerTests
{
    private static UpdateGoodsReturnCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static UpdateGoodsReturnCommand Command(Guid id, string branchId, Guid warehouseId, Guid supplierId, Guid itemId, Guid uomId, decimal qty = 6) =>
        new(id, branchId, warehouseId, supplierId, null, "DAMAGED", DateTimeOffset.UtcNow, "revised", null, "encoder",
            [new GoodsReturnLineInput(itemId, null, null, qty, uomId, 55, null)]);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, GoodsReturn goodsReturn, string branchId, Guid warehouseId, Guid supplierId, Guid itemId, Guid uomId)> Seed(string status = "DRAFT")
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
        var goodsReturn = PurchasingTestFixtures.GoodsReturn(branch.Id, warehouse.Id, supplier.Id, item.Id, uom.Id, status: status);
        db.GoodsReturns.Add(goodsReturn);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, goodsReturn, branch.Id, warehouse.Id, supplier.Id, item.Id, uom.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Update_Draft_Return()
    {
        var (db, goodsReturn, branchId, warehouseId, supplierId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(goodsReturn.Id, branchId, warehouseId, supplierId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Lines.Should().ContainSingle(l => l.QtyReturned == 6);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(Command(Guid.NewGuid(), "br-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, goodsReturn, branchId, warehouseId, supplierId, itemId, uomId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("GOODS_RETURNS", FormAction.Edit, branchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(Command(goodsReturn.Id, branchId, warehouseId, supplierId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, goodsReturn, branchId, warehouseId, supplierId, itemId, uomId) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(Command(goodsReturn.Id, branchId, warehouseId, supplierId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReturn.NotDraft");
        await db.DisposeAsync();
    }
}
