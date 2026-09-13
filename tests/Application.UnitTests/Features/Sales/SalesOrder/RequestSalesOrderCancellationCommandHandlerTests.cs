namespace ZARI.Application.UnitTests.Features.Sales.SalesOrder;

using ZARI.Application.Features.Sales.SalesOrders.RequestCancellation;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class RequestSalesOrderCancellationCommandHandlerTests
{
    private static RequestSalesOrderCancellationCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new SubmitForApprovalCommandHandler(db), new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, SalesOrder order, Guid customerId)> Seed(string status = "POSTED")
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var customer = LoanTestFixtures.Customer(branch.Id);
        db.Customers.Add(customer);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var order = SalesTestFixtures.SalesOrder(branch.Id, customer.Id, item.Id, uom.Id, status: status);
        db.SalesOrders.Add(order);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, order, customer.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Request_Cancellation_Of_Posted_Order()
    {
        var (db, order, _) = await Seed();

        var result = await Handler(db).HandleAsync(new RequestSalesOrderCancellationCommand(order.Id, "manager", "wrong item"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("PENDING_CANCELLATION");
        db.ApprovalRequests.Should().ContainSingle(r => r.EntityType == "SALES_ORDER" && r.RequestType == "CANCEL");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new RequestSalesOrderCancellationCommand(Guid.NewGuid(), "manager", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, order, _) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("SALES_ORDERS", FormAction.Cancel, order.BranchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new RequestSalesOrderCancellationCommand(order.Id, "manager", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Posted()
    {
        var (db, order, _) = await Seed(status: "DRAFT");

        var result = await Handler(db).HandleAsync(new RequestSalesOrderCancellationCommand(order.Id, "manager", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("SalesOrder.NotPosted");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Has_Posted_Delivery()
    {
        var (db, order, customerId) = await Seed();
        var warehouse = InventoryTestFixtures.Warehouse(order.BranchId);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.DeliveryOrders.Add(new DeliveryOrder
        {
            DoNo = "DO-0001", BranchId = order.BranchId, WarehouseId = warehouse.Id, CustomerId = customerId,
            DeliveryDate = DateTimeOffset.UtcNow, Status = "POSTED",
            Lines = [new DeliveryOrderLine { ItemId = order.Lines[0].ItemId, QtyShipped = order.Lines[0].Qty, UomId = order.Lines[0].UomId, UnitCost = 40, SalesOrderLineId = order.Lines[0].Id }]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new RequestSalesOrderCancellationCommand(order.Id, "manager", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("SalesOrder.HasPostedDelivery");
        await db.DisposeAsync();
    }
}
