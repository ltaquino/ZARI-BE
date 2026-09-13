namespace ZARI.Application.UnitTests.Features.Purchasing.PurchaseOrder;

using ZARI.Application.Features.Purchasing.PurchaseOrders.GetAllPaged;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetAllPurchaseOrdersPagedQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_A_Page_Of_Orders()
    {
        await using var db = TestDbContextFactory.Create();
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
        for (var i = 0; i < 3; i++) db.PurchaseOrders.Add(PurchasingTestFixtures.PurchaseOrder(branch.Id, supplier.Id, item.Id, uom.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllPurchaseOrdersPagedQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllPurchaseOrdersPagedQuery(Page: 1, PageSize: 2), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value!.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task HandleAsync_Should_Filter_By_Search()
    {
        await using var db = TestDbContextFactory.Create();
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
        var order = PurchasingTestFixtures.PurchaseOrder(branch.Id, supplier.Id, item.Id, uom.Id);
        order.PoNo = "PO-FINDME";
        db.PurchaseOrders.Add(order);
        db.PurchaseOrders.Add(PurchasingTestFixtures.PurchaseOrder(branch.Id, supplier.Id, item.Id, uom.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllPurchaseOrdersPagedQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllPurchaseOrdersPagedQuery(Page: 1, PageSize: 20, Search: "FINDME"), TestContext.Current.CancellationToken);

        result.Value!.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("PURCHASE_ORDERS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllPurchaseOrdersPagedQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllPurchaseOrdersPagedQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
