namespace ZARI.Application.UnitTests.Features.Sales.SalesReturn;

using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Inventory.SerialNumbers.ReverseIssue;
using ZARI.Application.Features.Inventory.StockLedgers.Receive;
using ZARI.Application.Features.Sales.SalesReturns.Create;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

/// <summary>
/// Only the DRAFT-creation path (guard clauses + normal Create) is exercised here — the
/// Company.SalesReturnQuickPostEnabled quick-post branch calls SalesReturnPostingService, which
/// opens a real ReceiveStockCommand transaction the InMemory provider rejects (same finding as
/// DeliveryOrder/GoodsReceiptPo), so quick-post's actual success path can't be exercised in this
/// test stack. No test here seeds a Company row with the toggle on.
/// </summary>
public sealed class CreateSalesReturnCommandHandlerTests
{
    private static CreateSalesReturnCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new GetNextDocumentNumberCommandHandler(db),
            LoanTestFixtures.SuccessHandler<ReceiveStockCommand, Result<ReceiveStockResponse>>(Result.Failure<ReceiveStockResponse>(Error.Failure("x", "unused"))),
            LoanTestFixtures.SuccessHandler<ReverseIssueSerialCommand, Result>(Result.Failure(Error.Failure("x", "unused"))),
            LoanTestFixtures.SuccessHandler<PostGlJournalCommand, Result<GlJournalResponse>>(Result.Failure<GlJournalResponse>(Error.Failure("x", "unused"))),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static CreateSalesReturnCommand Command(string branchId, Guid warehouseId, Guid customerId, Guid itemId, Guid uomId, Guid? deliveryOrderId = null, Guid? deliveryOrderLineId = null, decimal qty = 3) =>
        new(branchId, warehouseId, customerId, deliveryOrderId, DateTimeOffset.UtcNow, "damaged", null, "encoder", [new SalesReturnLineInput(itemId, qty, uomId, 100, deliveryOrderLineId, "VATABLE")]);

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
    public async Task HandleAsync_Should_Create_Draft_Return()
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
        permissions.HasPermissionOnBranchAsync("SALES_RETURNS", FormAction.Create, branchId, Arg.Any<CancellationToken>()).Returns(false);

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
    public async Task HandleAsync_Should_Fail_When_Delivery_Order_Not_Found()
    {
        var (db, branchId, warehouseId, customerId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, customerId, itemId, uomId, deliveryOrderId: Guid.NewGuid(), deliveryOrderLineId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("DeliveryOrder.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Delivery_Order_Not_Posted()
    {
        var (db, branchId, warehouseId, customerId, itemId, uomId) = await Seed();
        var doOrder = SalesTestFixtures.DeliveryOrder(branchId, warehouseId, customerId, itemId, uomId, status: "DRAFT");
        db.DeliveryOrders.Add(doOrder);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, customerId, itemId, uomId, deliveryOrderId: doOrder.Id, deliveryOrderLineId: doOrder.Lines[0].Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("SalesReturn.DeliveryOrderNotPosted");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Unexpected_Delivery_Order_Line()
    {
        var (db, branchId, warehouseId, customerId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, customerId, itemId, uomId, deliveryOrderId: null, deliveryOrderLineId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("SalesReturn.UnexpectedDeliveryOrderLine");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Line_Missing_Delivery_Order_Line()
    {
        var (db, branchId, warehouseId, customerId, itemId, uomId) = await Seed();
        var doOrder = SalesTestFixtures.DeliveryOrder(branchId, warehouseId, customerId, itemId, uomId, status: "POSTED");
        db.DeliveryOrders.Add(doOrder);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, customerId, itemId, uomId, deliveryOrderId: doOrder.Id, deliveryOrderLineId: null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("SalesReturn.LineMissingDeliveryOrderLine");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Exceeds_Delivered_Qty()
    {
        var (db, branchId, warehouseId, customerId, itemId, uomId) = await Seed();
        var doOrder = SalesTestFixtures.DeliveryOrder(branchId, warehouseId, customerId, itemId, uomId, status: "POSTED", qty: 10);
        db.DeliveryOrders.Add(doOrder);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, customerId, itemId, uomId, deliveryOrderId: doOrder.Id, deliveryOrderLineId: doOrder.Lines[0].Id, qty: 20), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("SalesReturn.ExceedsDeliveredQty");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Return_Against_Posted_Delivery()
    {
        var (db, branchId, warehouseId, customerId, itemId, uomId) = await Seed();
        var doOrder = SalesTestFixtures.DeliveryOrder(branchId, warehouseId, customerId, itemId, uomId, status: "POSTED", qty: 10);
        db.DeliveryOrders.Add(doOrder);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, warehouseId, customerId, itemId, uomId, deliveryOrderId: doOrder.Id, deliveryOrderLineId: doOrder.Lines[0].Id, qty: 4), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DeliveryOrderId.Should().Be(doOrder.Id);
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
