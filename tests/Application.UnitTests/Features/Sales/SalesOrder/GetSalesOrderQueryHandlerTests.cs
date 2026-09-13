namespace ZARI.Application.UnitTests.Features.Sales.SalesOrder;

using ZARI.Application.Features.Sales.SalesOrders.Get;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetSalesOrderQueryHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.SalesOrder order)> Seed()
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
        var order = SalesTestFixtures.SalesOrder(branch.Id, customer.Id, item.Id, uom.Id);
        db.SalesOrders.Add(order);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, order);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Order_When_Found()
    {
        var (db, order) = await Seed();
        var handler = new GetSalesOrderQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetSalesOrderQuery(order.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Lines.Should().ContainSingle();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetSalesOrderQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetSalesOrderQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, order) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("SALES_ORDERS", FormAction.View, order.BranchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetSalesOrderQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetSalesOrderQuery(order.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}
