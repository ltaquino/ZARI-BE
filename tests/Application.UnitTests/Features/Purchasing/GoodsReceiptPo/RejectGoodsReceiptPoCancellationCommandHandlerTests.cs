namespace ZARI.Application.UnitTests.Features.Purchasing.GoodsReceiptPo;

using ZARI.Application.Features.Purchasing.GoodsReceiptPos.RejectCancellation;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class RejectGoodsReceiptPoCancellationCommandHandlerTests
{
    private static RejectGoodsReceiptPoCancellationCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(Dummy())),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "GOODS_RECEIPT_PO", "x", "br-1", "user", DateTimeOffset.UtcNow, "REJECTED", "CANCEL", null, [], DateTimeOffset.UtcNow);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, GoodsReceiptPo receipt)> Seed(string status = "PENDING_CANCELLATION", bool withApprovalRequest = true)
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
        receipt.CancelReason = "pending review";
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
        return (db, receipt);
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_Cancellation_Back_To_Posted()
    {
        var (db, receipt) = await Seed();

        var result = await Handler(db).HandleAsync(new RejectGoodsReceiptPoCancellationCommand(receipt.Id, "admin.hq", "not warranted"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("POSTED");
        result.Value!.CancelReason.Should().BeNull();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new RejectGoodsReceiptPoCancellationCommand(Guid.NewGuid(), "admin.hq", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, receipt) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasCancellationAuthorityAsync("GOODS_RECEIPT_PO", Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new RejectGoodsReceiptPoCancellationCommand(receipt.Id, "admin.hq", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Cancellation()
    {
        var (db, receipt) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(new RejectGoodsReceiptPoCancellationCommand(receipt.Id, "admin.hq", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReceiptPo.NotPendingCancellation");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Cancellation_Request_Found()
    {
        var (db, receipt) = await Seed(withApprovalRequest: false);

        var result = await Handler(db).HandleAsync(new RejectGoodsReceiptPoCancellationCommand(receipt.Id, "admin.hq", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApprovalRequest.NotFound");
        await db.DisposeAsync();
    }
}
