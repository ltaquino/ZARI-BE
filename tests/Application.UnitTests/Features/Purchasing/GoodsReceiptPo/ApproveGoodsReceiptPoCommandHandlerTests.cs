namespace ZARI.Application.UnitTests.Features.Purchasing.GoodsReceiptPo;

using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Inventory.SerialNumbers.GetAll;
using ZARI.Application.Features.Inventory.SerialNumbers.Receive;
using ZARI.Application.Features.Inventory.StockLedgers.Receive;
using ZARI.Application.Features.Inventory.StockLocationBalances.Receive;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.Approve;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// The real success path (receive stock -> post GRNI journal -> flip status) is not InMemory-
/// testable: ReceiveStockCommandHandler opens a real `Database.BeginTransactionAsync`, which the
/// InMemory provider rejects outright (confirmed directly), and even with that faked, the
/// handler's own final status flip uses `ExecuteUpdateAsync` (also unsupported). So every
/// dependency here is faked and only the guard clauses / pre-decide re-check are exercised.
/// </summary>
public sealed class ApproveGoodsReceiptPoCommandHandlerTests
{
    private static ApproveGoodsReceiptPoCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db,
            LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(Dummy())),
            LoanTestFixtures.SuccessHandler<ReceiveStockCommand, Result<ReceiveStockResponse>>(Result.Success(new ReceiveStockResponse(50))),
            LoanTestFixtures.SuccessHandler<ReceiveSerialCommand, Result<SerialNumberResponse>>(Result.Failure<SerialNumberResponse>(Error.Failure("x", "unused"))),
            LoanTestFixtures.SuccessHandler<ReceiveIntoLocationCommand, Result>(Result.Success()),
            LoanTestFixtures.SuccessHandler<PostGlJournalCommand, Result<GlJournalResponse>>(Result.Failure<GlJournalResponse>(Error.Failure("x", "unused"))),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "GOODS_RECEIPT_PO", "x", "br-1", "user", DateTimeOffset.UtcNow, "APPROVED", "SUBMIT", null, [], DateTimeOffset.UtcNow);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, GoodsReceiptPo receipt)> Seed(string status = "PENDING_APPROVAL", bool withApprovalRequest = true)
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        var supplier = PurchasingTestFixtures.Supplier();
        db.Suppliers.Add(supplier);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var receipt = PurchasingTestFixtures.GoodsReceiptPo(branch.Id, warehouse.Id, supplier.Id, item.Id, uom.Id, status: status);
        db.GoodsReceiptPos.Add(receipt);
        if (withApprovalRequest)
        {
            db.ApprovalRequests.Add(new ApprovalRequest
            {
                EntityType = "GOODS_RECEIPT_PO", EntityId = receipt.Id.ToString(), BranchId = branch.Id,
                RequestedBy = "encoder", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT"
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, receipt);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new ApproveGoodsReceiptPoCommand(Guid.NewGuid(), "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, receipt) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("GOODS_RECEIPT_PO", FormAction.Approve, receipt.BranchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new ApproveGoodsReceiptPoCommand(receipt.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Approval()
    {
        var (db, receipt) = await Seed(status: "DRAFT");

        var result = await Handler(db).HandleAsync(new ApproveGoodsReceiptPoCommand(receipt.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReceiptPo.NotPendingApproval");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Approval_Request_Found()
    {
        var (db, receipt) = await Seed(withApprovalRequest: false);

        var result = await Handler(db).HandleAsync(new ApproveGoodsReceiptPoCommand(receipt.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApprovalRequest.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Exceeds_Ordered_Qty_On_Reapproval_Race()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        var supplier = PurchasingTestFixtures.Supplier();
        db.Suppliers.Add(supplier);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var po = PurchasingTestFixtures.PurchaseOrder(branch.Id, supplier.Id, item.Id, uom.Id, status: "POSTED", qty: 10);
        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Another receipt already claimed 8 of the 10 ordered.
        var otherReceipt = PurchasingTestFixtures.GoodsReceiptPo(branch.Id, warehouse.Id, supplier.Id, item.Id, uom.Id, status: "POSTED", qty: 8, purchaseOrderId: po.Id, purchaseOrderLineId: po.Lines[0].Id);
        db.GoodsReceiptPos.Add(otherReceipt);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // This receipt (pending approval) claims 5 more — 8 + 5 > 10.
        var receipt = PurchasingTestFixtures.GoodsReceiptPo(branch.Id, warehouse.Id, supplier.Id, item.Id, uom.Id, status: "PENDING_APPROVAL", qty: 5, purchaseOrderId: po.Id, purchaseOrderLineId: po.Lines[0].Id);
        db.GoodsReceiptPos.Add(receipt);
        db.ApprovalRequests.Add(new ApprovalRequest
        {
            EntityType = "GOODS_RECEIPT_PO", EntityId = receipt.Id.ToString(), BranchId = branch.Id,
            RequestedBy = "encoder", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new ApproveGoodsReceiptPoCommand(receipt.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReceiptPo.ExceedsOrderedQty");
        await db.DisposeAsync();
    }
}
