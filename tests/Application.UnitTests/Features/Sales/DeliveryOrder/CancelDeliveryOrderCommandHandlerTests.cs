namespace ZARI.Application.UnitTests.Features.Sales.DeliveryOrder;

using ZARI.Application.Features.Sales.DeliveryOrders.Cancel;
using ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CancelDeliveryOrderCommandHandlerTests
{
    private static CancelDeliveryOrderCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, LoanTestFixtures.SuccessHandler<CancelPendingApprovalRequestCommand, Result>(Result.Success()),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.DeliveryOrder order)> Seed(string status = "DRAFT")
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
        return (db, order);
    }

    [Fact]
    public async Task HandleAsync_Should_Cancel_Draft_Delivery()
    {
        var (db, order) = await Seed();

        var result = await Handler(db).HandleAsync(new CancelDeliveryOrderCommand(order.Id, "encoder", "no longer needed"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("CANCELLED");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new CancelDeliveryOrderCommand(Guid.NewGuid(), "encoder", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, order) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("DELIVERIES", FormAction.Cancel, order.BranchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new CancelDeliveryOrderCommand(order.Id, "encoder", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Already_Cancelled()
    {
        var (db, order) = await Seed(status: "CANCELLED");

        var result = await Handler(db).HandleAsync(new CancelDeliveryOrderCommand(order.Id, "encoder", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("DeliveryOrder.AlreadyCancelled");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Posted_Requires_Cancellation_Request()
    {
        var (db, order) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(new CancelDeliveryOrderCommand(order.Id, "encoder", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("DeliveryOrder.RequiresCancellationRequest");
        await db.DisposeAsync();
    }
}
