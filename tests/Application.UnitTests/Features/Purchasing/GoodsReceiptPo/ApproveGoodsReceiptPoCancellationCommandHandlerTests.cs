namespace ZARI.Application.UnitTests.Features.Purchasing.GoodsReceiptPo;

using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Reverse;
using ZARI.Application.Features.Inventory.SerialNumbers.ReverseReceive;
using ZARI.Application.Features.Inventory.StockLedgers.Reverse;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.ApproveCancellation;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Same InMemory gap as ApproveGoodsReceiptPoCommandHandlerTests: the final status flip uses
/// ExecuteUpdateAsync, so only guard clauses and the pre-decide downstream re-check are exercised
/// here — every reversal dependency is faked and never needs to actually run.
/// </summary>
public sealed class ApproveGoodsReceiptPoCancellationCommandHandlerTests
{
    private static ApproveGoodsReceiptPoCancellationCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db,
            LoanTestFixtures.SuccessHandler<ReverseStockMovementsCommand, Result>(Result.Success()),
            LoanTestFixtures.SuccessHandler<ReverseReceiveSerialCommand, Result>(Result.Success()),
            LoanTestFixtures.SuccessHandler<ReverseGlJournalsCommand, Result<List<GlJournalResponse>>>(Result.Success(new List<GlJournalResponse>())),
            LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(Dummy())),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "GOODS_RECEIPT_PO", "x", "br-1", "user", DateTimeOffset.UtcNow, "APPROVED", "CANCEL", null, [], DateTimeOffset.UtcNow);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, GoodsReceiptPo receipt, Guid supplierId, string branchId)> Seed(string status = "PENDING_CANCELLATION", bool withApprovalRequest = true)
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
                RequestedBy = "manager", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "CANCEL"
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, receipt, supplier.Id, branch.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new ApproveGoodsReceiptPoCancellationCommand(Guid.NewGuid(), "admin.hq", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, receipt, _, _) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasCancellationAuthorityAsync("GOODS_RECEIPT_PO", Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new ApproveGoodsReceiptPoCancellationCommand(receipt.Id, "admin.hq", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Cancellation()
    {
        var (db, receipt, _, _) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(new ApproveGoodsReceiptPoCancellationCommand(receipt.Id, "admin.hq", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReceiptPo.NotPendingCancellation");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Cancellation_Request_Found()
    {
        var (db, receipt, _, _) = await Seed(withApprovalRequest: false);

        var result = await Handler(db).HandleAsync(new ApproveGoodsReceiptPoCancellationCommand(receipt.Id, "admin.hq", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApprovalRequest.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Has_Posted_Ap_Invoice()
    {
        var (db, receipt, supplierId, branchId) = await Seed();
        db.ApInvoices.Add(new ApInvoice
        {
            InvoiceNo = "APINV-0001", BranchId = branchId, SupplierId = supplierId, InvoiceType = "ITEM",
            GoodsReceiptPoId = receipt.Id, SupplierInvoiceNo = "SINV-1", InvoiceDate = DateTimeOffset.UtcNow, Status = "POSTED",
            Lines = [new ApInvoiceLine { ItemId = receipt.Lines[0].ItemId, Qty = receipt.Lines[0].QtyReceived, UomId = receipt.Lines[0].UomId, UnitCost = receipt.Lines[0].UnitCost, GoodsReceiptPoLineId = receipt.Lines[0].Id }]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new ApproveGoodsReceiptPoCancellationCommand(receipt.Id, "admin.hq", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReceiptPo.HasPostedApInvoice");
        await db.DisposeAsync();
    }
}
