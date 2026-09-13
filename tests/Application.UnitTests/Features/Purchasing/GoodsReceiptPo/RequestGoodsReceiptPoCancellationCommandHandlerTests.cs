namespace ZARI.Application.UnitTests.Features.Purchasing.GoodsReceiptPo;

using ZARI.Application.Features.Purchasing.GoodsReceiptPos.RequestCancellation;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class RequestGoodsReceiptPoCancellationCommandHandlerTests
{
    private static RequestGoodsReceiptPoCancellationCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new SubmitForApprovalCommandHandler(db), new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, GoodsReceiptPo receipt, Guid supplierId, string branchId)> Seed(string status = "POSTED")
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
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, receipt, supplier.Id, branch.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Request_Cancellation_Of_Posted_Receipt()
    {
        var (db, receipt, _, _) = await Seed();

        var result = await Handler(db).HandleAsync(new RequestGoodsReceiptPoCancellationCommand(receipt.Id, "manager", "wrong supplier"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("PENDING_CANCELLATION");
        db.ApprovalRequests.Should().ContainSingle(r => r.EntityType == "GOODS_RECEIPT_PO" && r.RequestType == "CANCEL");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new RequestGoodsReceiptPoCancellationCommand(Guid.NewGuid(), "manager", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, receipt, _, _) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("GOODS_RECEIPT_PO", FormAction.Cancel, receipt.BranchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new RequestGoodsReceiptPoCancellationCommand(receipt.Id, "manager", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Posted()
    {
        var (db, receipt, _, _) = await Seed(status: "DRAFT");

        var result = await Handler(db).HandleAsync(new RequestGoodsReceiptPoCancellationCommand(receipt.Id, "manager", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReceiptPo.NotPosted");
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

        var result = await Handler(db).HandleAsync(new RequestGoodsReceiptPoCancellationCommand(receipt.Id, "manager", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReceiptPo.HasPostedApInvoice");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Has_Posted_Goods_Return()
    {
        var (db, receipt, supplierId, branchId) = await Seed();
        var warehouseId = receipt.WarehouseId;
        db.GoodsReturns.Add(new GoodsReturn
        {
            ReturnNo = "GR-0001", BranchId = branchId, WarehouseId = warehouseId, SupplierId = supplierId,
            GoodsReceiptPoId = receipt.Id, ReasonCode = "DAMAGED", ReturnDate = DateTimeOffset.UtcNow, Status = "POSTED",
            Lines = [new GoodsReturnLine { ItemId = receipt.Lines[0].ItemId, QtyReturned = receipt.Lines[0].QtyReceived, UomId = receipt.Lines[0].UomId, UnitCost = receipt.Lines[0].UnitCost, GoodsReceiptPoLineId = receipt.Lines[0].Id }]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new RequestGoodsReceiptPoCancellationCommand(receipt.Id, "manager", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReceiptPo.HasPostedGoodsReturn");
        await db.DisposeAsync();
    }
}
