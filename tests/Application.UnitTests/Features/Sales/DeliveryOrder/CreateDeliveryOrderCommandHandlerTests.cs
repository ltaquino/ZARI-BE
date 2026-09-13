namespace ZARI.Application.UnitTests.Features.Sales.DeliveryOrder;

using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Inventory.StockLedgers.Issue;
using ZARI.Application.Features.Sales.DeliveryOrders.Create;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

/// <summary>
/// Only the DRAFT-creation path (guard clauses + normal Create) is exercised here — the
/// Company.DeliveryQuickPostEnabled quick-post branch calls DeliveryPostingService, which opens a
/// real IssueStockLinesCommand transaction the InMemory provider rejects (same finding as
/// GoodsReceiptPo/GoodsReturn's Approve handlers), so quick-post's actual success path can't be
/// exercised in this test stack. No test here seeds a Company row with the toggle on.
/// </summary>
public sealed class CreateDeliveryOrderCommandHandlerTests
{
    private static CreateDeliveryOrderCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new GetNextDocumentNumberCommandHandler(db),
            LoanTestFixtures.SuccessHandler<IssueStockLinesCommand, Result<IssueStockLinesResponse>>(Result.Failure<IssueStockLinesResponse>(Error.Failure("x", "unused"))),
            LoanTestFixtures.SuccessHandler<PostGlJournalCommand, Result<GlJournalResponse>>(Result.Failure<GlJournalResponse>(Error.Failure("x", "unused"))),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static CreateDeliveryOrderCommand Command(string branchId, Guid warehouseId, Guid customerId, Guid itemId, Guid uomId, Guid? salesOrderId = null, Guid? salesOrderLineId = null, decimal qty = 5) =>
        new(branchId, warehouseId, customerId, salesOrderId, DateTimeOffset.UtcNow, "urgent", null, "encoder", [new DeliveryOrderLineInput(itemId, qty, uomId, salesOrderLineId)]);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, string branchId, Guid warehouseId, Guid customerId, Guid itemId, Guid uomId)> Seed()
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
        return (db, branch.Id, warehouse.Id, customer.Id, item.Id, uom.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Draft_Delivery()
    {
        var (db, branchId, warehouseId, customerId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, customerId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
        result.Value!.Lines.Should().ContainSingle();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branchId, warehouseId, customerId, itemId, uomId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("DELIVERIES", FormAction.Create, branchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(Command(branchId, warehouseId, customerId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        var (db, _, warehouseId, customerId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command("br-missing", warehouseId, customerId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Warehouse_Not_Found()
    {
        var (db, branchId, _, customerId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, Guid.NewGuid(), customerId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Warehouse.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Customer_Not_Found()
    {
        var (db, branchId, warehouseId, _, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, Guid.NewGuid(), itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Customer.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Item_Not_Found()
    {
        var (db, branchId, warehouseId, customerId, _, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, customerId, Guid.NewGuid(), uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Item.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Uom_Not_Found()
    {
        var (db, branchId, warehouseId, customerId, itemId, _) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, customerId, itemId, Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Uom.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Sales_Order_Not_Found()
    {
        var (db, branchId, warehouseId, customerId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, customerId, itemId, uomId, salesOrderId: Guid.NewGuid(), salesOrderLineId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("SalesOrder.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Sales_Order_Not_Posted()
    {
        var (db, branchId, warehouseId, customerId, itemId, uomId) = await Seed();
        var so = SalesTestFixtures.SalesOrder(branchId, customerId, itemId, uomId, status: "DRAFT");
        db.SalesOrders.Add(so);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, customerId, itemId, uomId, salesOrderId: so.Id, salesOrderLineId: so.Lines[0].Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("DeliveryOrder.SalesOrderNotPosted");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Unexpected_Sales_Order_Line()
    {
        var (db, branchId, warehouseId, customerId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, customerId, itemId, uomId, salesOrderId: null, salesOrderLineId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("DeliveryOrder.UnexpectedSalesOrderLine");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Line_Missing_Sales_Order_Line()
    {
        var (db, branchId, warehouseId, customerId, itemId, uomId) = await Seed();
        var so = SalesTestFixtures.SalesOrder(branchId, customerId, itemId, uomId, status: "POSTED");
        db.SalesOrders.Add(so);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, customerId, itemId, uomId, salesOrderId: so.Id, salesOrderLineId: null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("DeliveryOrder.LineMissingSalesOrderLine");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Exceeds_Ordered_Qty()
    {
        var (db, branchId, warehouseId, customerId, itemId, uomId) = await Seed();
        var so = SalesTestFixtures.SalesOrder(branchId, customerId, itemId, uomId, status: "POSTED", qty: 10);
        db.SalesOrders.Add(so);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, customerId, itemId, uomId, salesOrderId: so.Id, salesOrderLineId: so.Lines[0].Id, qty: 20), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("DeliveryOrder.ExceedsOrderedQty");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Delivery_Against_Posted_Sales_Order()
    {
        var (db, branchId, warehouseId, customerId, itemId, uomId) = await Seed();
        var so = SalesTestFixtures.SalesOrder(branchId, customerId, itemId, uomId, status: "POSTED", qty: 10);
        db.SalesOrders.Add(so);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, customerId, itemId, uomId, salesOrderId: so.Id, salesOrderLineId: so.Lines[0].Id, qty: 5), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SalesOrderId.Should().Be(so.Id);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Cost_Center_Not_Found()
    {
        var (db, branchId, warehouseId, customerId, itemId, uomId) = await Seed();
        var command = Command(branchId, warehouseId, customerId, itemId, uomId) with { CostCenterId = Guid.NewGuid() };

        var result = await Handler(db).HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CostCenter.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Not_Quick_Post_When_Toggle_Disabled()
    {
        var (db, branchId, warehouseId, customerId, itemId, uomId) = await Seed();
        db.Companies.Add(SystemModuleTestFixtures.Company());
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, customerId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
        await db.DisposeAsync();
    }
}
