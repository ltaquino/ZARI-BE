namespace ZARI.Application.UnitTests.Features.Sales.SalesOrder;

using ZARI.Application.Features.Sales.SalesOrders.Delete;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class DeleteSalesOrderCommandHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.SalesOrder order)> Seed(string status = "DRAFT")
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
        return (db, order);
    }

    [Fact]
    public async Task HandleAsync_Should_Delete_Draft_Order()
    {
        var (db, order) = await Seed();
        var handler = new DeleteSalesOrderCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteSalesOrderCommand(order.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.SalesOrders.Should().BeEmpty();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteSalesOrderCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteSalesOrderCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, order) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("SALES_ORDERS", FormAction.Delete, order.BranchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteSalesOrderCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeleteSalesOrderCommand(order.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, order) = await Seed(status: "POSTED");
        var handler = new DeleteSalesOrderCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteSalesOrderCommand(order.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("SalesOrder.NotDraft");
        await db.DisposeAsync();
    }
}
