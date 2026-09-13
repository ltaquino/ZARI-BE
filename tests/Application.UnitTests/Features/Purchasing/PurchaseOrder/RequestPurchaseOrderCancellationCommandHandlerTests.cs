namespace ZARI.Application.UnitTests.Features.Purchasing.PurchaseOrder;

using ZARI.Application.Features.Purchasing.PurchaseOrders.RequestCancellation;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class RequestPurchaseOrderCancellationCommandHandlerTests
{
    private static RequestPurchaseOrderCancellationCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new SubmitForApprovalCommandHandler(db), new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, PurchaseOrder order, Guid supplierId)> Seed(string status = "POSTED")
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
        return (db, order, supplier.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Request_Cancellation_Of_Posted_Order()
    {
        var (db, order, _) = await Seed();

        var result = await Handler(db).HandleAsync(new RequestPurchaseOrderCancellationCommand(order.Id, "manager", "wrong supplier"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("PENDING_CANCELLATION");
        db.ApprovalRequests.Should().ContainSingle(r => r.EntityType == "PURCHASE_ORDER" && r.RequestType == "CANCEL");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new RequestPurchaseOrderCancellationCommand(Guid.NewGuid(), "manager", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, order, _) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("PURCHASE_ORDERS", FormAction.Cancel, order.BranchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new RequestPurchaseOrderCancellationCommand(order.Id, "manager", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Posted()
    {
        var (db, order, _) = await Seed(status: "DRAFT");

        var result = await Handler(db).HandleAsync(new RequestPurchaseOrderCancellationCommand(order.Id, "manager", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PurchaseOrder.NotPosted");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Has_Posted_Receipt()
    {
        var (db, order, supplierId) = await Seed();
        var warehouse = InventoryTestFixtures.Warehouse(order.BranchId);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.GoodsReceiptPos.Add(new GoodsReceiptPo
        {
            GrpoNo = "GRPO-0001", BranchId = order.BranchId, WarehouseId = warehouse.Id, SupplierId = supplierId,
            ReceiptDate = DateTimeOffset.UtcNow, Status = "POSTED",
            Lines = [new GoodsReceiptPoLine { ItemId = order.Lines[0].ItemId, QtyReceived = order.Lines[0].Qty, UomId = order.Lines[0].UomId, UnitCost = order.Lines[0].UnitCost, PurchaseOrderLineId = order.Lines[0].Id }]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new RequestPurchaseOrderCancellationCommand(order.Id, "manager", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PurchaseOrder.HasPostedReceipt");
        await db.DisposeAsync();
    }
}
