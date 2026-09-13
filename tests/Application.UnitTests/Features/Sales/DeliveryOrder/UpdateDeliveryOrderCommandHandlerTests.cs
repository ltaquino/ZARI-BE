namespace ZARI.Application.UnitTests.Features.Sales.DeliveryOrder;

using ZARI.Application.Features.Sales.DeliveryOrders.Create;
using ZARI.Application.Features.Sales.DeliveryOrders.Update;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class UpdateDeliveryOrderCommandHandlerTests
{
    private static UpdateDeliveryOrderCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static UpdateDeliveryOrderCommand Command(Guid id, string branchId, Guid warehouseId, Guid customerId, Guid itemId, Guid uomId) =>
        new(id, branchId, warehouseId, customerId, null, DateTimeOffset.UtcNow, "updated", null, "encoder", [new DeliveryOrderLineInput(itemId, 3, uomId, null)]);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, string branchId, Guid warehouseId, Guid customerId, Guid itemId, Guid uomId, ZARI.Domain.Entities.DeliveryOrder order)> Seed(string status = "DRAFT")
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        var customer = LoanTestFixtures.Customer(branch.Id);
        db.Customers.Add(customer);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var order = SalesTestFixtures.DeliveryOrder(branch.Id, warehouse.Id, customer.Id, item.Id, uom.Id, status: status);
        db.DeliveryOrders.Add(order);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch.Id, warehouse.Id, customer.Id, item.Id, uom.Id, order);
    }

    [Fact]
    public async Task HandleAsync_Should_Update_Draft_Delivery()
    {
        var (db, branchId, warehouseId, customerId, itemId, uomId, order) = await Seed();

        var result = await Handler(db).HandleAsync(Command(order.Id, branchId, warehouseId, customerId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Remarks.Should().Be("updated");
        result.Value!.Lines.Should().ContainSingle(l => l.QtyShipped == 3);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        var (db, branchId, warehouseId, customerId, itemId, uomId, _) = await Seed();

        var result = await Handler(db).HandleAsync(Command(Guid.NewGuid(), branchId, warehouseId, customerId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branchId, warehouseId, customerId, itemId, uomId, order) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("DELIVERIES", FormAction.Edit, branchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(Command(order.Id, branchId, warehouseId, customerId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, branchId, warehouseId, customerId, itemId, uomId, order) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(Command(order.Id, branchId, warehouseId, customerId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("DeliveryOrder.NotDraft");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Customer_Not_Found()
    {
        var (db, branchId, warehouseId, _, itemId, uomId, order) = await Seed();

        var result = await Handler(db).HandleAsync(Command(order.Id, branchId, warehouseId, Guid.NewGuid(), itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Customer.NotFound");
        await db.DisposeAsync();
    }
}
