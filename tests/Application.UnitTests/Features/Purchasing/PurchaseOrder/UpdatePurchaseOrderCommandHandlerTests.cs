namespace ZARI.Application.UnitTests.Features.Purchasing.PurchaseOrder;

using ZARI.Application.Features.Purchasing.PurchaseOrders.Create;
using ZARI.Application.Features.Purchasing.PurchaseOrders.Update;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdatePurchaseOrderCommandHandlerTests
{
    private static UpdatePurchaseOrderCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static UpdatePurchaseOrderCommand Command(Guid id, string branchId, Guid supplierId, Guid itemId, Guid uomId, decimal qty = 8) =>
        new(id, branchId, supplierId, DateTimeOffset.UtcNow, null, "revised", null, "encoder", [new PurchaseOrderLineInput(itemId, qty, uomId, 55, null)]);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, PurchaseOrder order, string branchId, Guid supplierId, Guid itemId, Guid uomId)> Seed(string status = "DRAFT")
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var supplier = PurchasingTestFixtures.Supplier();
        db.Suppliers.Add(supplier);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var order = PurchasingTestFixtures.PurchaseOrder(branch.Id, supplier.Id, item.Id, uom.Id, status: status);
        db.PurchaseOrders.Add(order);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, order, branch.Id, supplier.Id, item.Id, uom.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Update_Draft_Order()
    {
        var (db, order, branchId, supplierId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(order.Id, branchId, supplierId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Lines.Should().ContainSingle(l => l.Qty == 8);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(Command(Guid.NewGuid(), "br-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, order, branchId, supplierId, itemId, uomId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("PURCHASE_ORDERS", FormAction.Edit, branchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(Command(order.Id, branchId, supplierId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, order, branchId, supplierId, itemId, uomId) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(Command(order.Id, branchId, supplierId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PurchaseOrder.NotDraft");
        await db.DisposeAsync();
    }
}
