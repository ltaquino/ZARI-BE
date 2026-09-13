namespace ZARI.Application.UnitTests.Features.Sales.DeliveryOrder;

using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Reverse;
using ZARI.Application.Features.Inventory.StockLedgers.Reverse;
using ZARI.Application.Features.Sales.DeliveryOrders.ApproveCancellation;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Like ApproveDeliveryOrderCommandHandlerTests, the real reversal path (reverse stock -> reverse
/// GL -> flip status) is not InMemory-testable: ReverseStockMovementsCommandHandler opens a real
/// transaction, and the handler's own final status flip uses ExecuteUpdateAsync. Every dependency
/// is faked and only the guard clauses / pre-decide re-check are exercised.
/// </summary>
public sealed class ApproveDeliveryOrderCancellationCommandHandlerTests
{
    private static ApproveDeliveryOrderCancellationCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db,
            LoanTestFixtures.SuccessHandler<ReverseStockMovementsCommand, Result>(Result.Failure(Error.Failure("x", "unused"))),
            LoanTestFixtures.SuccessHandler<ReverseGlJournalsCommand, Result<List<GlJournalResponse>>>(Result.Failure<List<GlJournalResponse>>(Error.Failure("x", "unused"))),
            LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(Dummy())),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "DELIVERY_ORDER", "x", "br-1", "user", DateTimeOffset.UtcNow, "APPROVED", "CANCEL", null, [], DateTimeOffset.UtcNow);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, DeliveryOrder order, Guid customerId)> Seed(string status = "PENDING_CANCELLATION", bool withApprovalRequest = true)
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
        if (withApprovalRequest)
        {
            db.ApprovalRequests.Add(new ApprovalRequest
            {
                EntityType = "DELIVERY_ORDER", EntityId = order.Id.ToString(), BranchId = branch.Id,
                RequestedBy = "manager", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "CANCEL"
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, order, customer.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new ApproveDeliveryOrderCancellationCommand(Guid.NewGuid(), "admin.hq", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, order, _) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasCancellationAuthorityAsync("DELIVERIES", Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new ApproveDeliveryOrderCancellationCommand(order.Id, "admin.hq", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Cancellation()
    {
        var (db, order, _) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(new ApproveDeliveryOrderCancellationCommand(order.Id, "admin.hq", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("DeliveryOrder.NotPendingCancellation");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Cancellation_Request_Found()
    {
        var (db, order, _) = await Seed(withApprovalRequest: false);

        var result = await Handler(db).HandleAsync(new ApproveDeliveryOrderCancellationCommand(order.Id, "admin.hq", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApprovalRequest.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Has_Posted_Sales_Invoice()
    {
        var (db, order, customerId) = await Seed();
        var invoice = new SalesInvoice
        {
            InvoiceNo = "SINV-0001", BranchId = order.BranchId, CustomerId = customerId,
            InvoiceDate = DateTimeOffset.UtcNow, Status = "POSTED",
            Lines = [new SalesInvoiceLine { ItemId = order.Lines[0].ItemId, Qty = order.Lines[0].QtyShipped, UomId = order.Lines[0].UomId, UnitPrice = 100, DeliveryOrderLineId = order.Lines[0].Id }]
        };
        db.SalesInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new ApproveDeliveryOrderCancellationCommand(order.Id, "admin.hq", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("DeliveryOrder.HasPostedSalesInvoice");
        await db.DisposeAsync();
    }
}
