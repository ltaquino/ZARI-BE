namespace ZARI.Application.UnitTests.Features.Purchasing.GoodsReturn;

using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Inventory.SerialNumbers.Issue;
using ZARI.Application.Features.Inventory.StockLedgers.Issue;
using ZARI.Application.Features.Purchasing.GoodsReturns.Approve;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Same InMemory gap as ApproveGoodsReceiptPoCommandHandlerTests: IssueStockLinesCommandHandler
/// opens a real Database transaction (unsupported by the InMemory provider) and the handler's own
/// final status flip uses ExecuteUpdateAsync. Every dependency is faked; only guard clauses and the
/// pre-decide re-check are exercised.
/// </summary>
public sealed class ApproveGoodsReturnCommandHandlerTests
{
    private static ApproveGoodsReturnCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db,
            LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(Dummy())),
            LoanTestFixtures.SuccessHandler<IssueStockLinesCommand, Result<IssueStockLinesResponse>>(Result.Success(new IssueStockLinesResponse(new Dictionary<string, decimal>()))),
            LoanTestFixtures.SuccessHandler<IssueSerialCommand, Result>(Result.Success()),
            LoanTestFixtures.SuccessHandler<PostGlJournalCommand, Result<GlJournalResponse>>(Result.Failure<GlJournalResponse>(Error.Failure("x", "unused"))),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "GOODS_RETURNS", "x", "br-1", "user", DateTimeOffset.UtcNow, "APPROVED", "SUBMIT", null, [], DateTimeOffset.UtcNow);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, GoodsReturn goodsReturn)> Seed(string status = "PENDING_APPROVAL", bool withApprovalRequest = true)
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
        var goodsReturn = PurchasingTestFixtures.GoodsReturn(branch.Id, warehouse.Id, supplier.Id, item.Id, uom.Id, status: status);
        db.GoodsReturns.Add(goodsReturn);
        if (withApprovalRequest)
        {
            db.ApprovalRequests.Add(new ApprovalRequest
            {
                EntityType = "GOODS_RETURNS", EntityId = goodsReturn.Id.ToString(), BranchId = branch.Id,
                RequestedBy = "encoder", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT"
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, goodsReturn);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new ApproveGoodsReturnCommand(Guid.NewGuid(), "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, goodsReturn) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("GOODS_RETURNS", FormAction.Approve, goodsReturn.BranchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new ApproveGoodsReturnCommand(goodsReturn.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Approval()
    {
        var (db, goodsReturn) = await Seed(status: "DRAFT");

        var result = await Handler(db).HandleAsync(new ApproveGoodsReturnCommand(goodsReturn.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReturn.NotPendingApproval");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Approval_Request_Found()
    {
        var (db, goodsReturn) = await Seed(withApprovalRequest: false);

        var result = await Handler(db).HandleAsync(new ApproveGoodsReturnCommand(goodsReturn.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApprovalRequest.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Exceeds_Received_Qty_On_Reapproval_Race()
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
        var grpo = PurchasingTestFixtures.GoodsReceiptPo(branch.Id, warehouse.Id, supplier.Id, item.Id, uom.Id, status: "POSTED", qty: 10);
        db.GoodsReceiptPos.Add(grpo);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Another return already claimed 8 of the 10 received.
        var otherReturn = PurchasingTestFixtures.GoodsReturn(branch.Id, warehouse.Id, supplier.Id, item.Id, uom.Id, status: "POSTED", qty: 8, goodsReceiptPoId: grpo.Id, goodsReceiptPoLineId: grpo.Lines[0].Id);
        db.GoodsReturns.Add(otherReturn);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // This return (pending approval) claims 5 more — 8 + 5 > 10.
        var goodsReturn = PurchasingTestFixtures.GoodsReturn(branch.Id, warehouse.Id, supplier.Id, item.Id, uom.Id, status: "PENDING_APPROVAL", qty: 5, goodsReceiptPoId: grpo.Id, goodsReceiptPoLineId: grpo.Lines[0].Id);
        db.GoodsReturns.Add(goodsReturn);
        db.ApprovalRequests.Add(new ApprovalRequest
        {
            EntityType = "GOODS_RETURNS", EntityId = goodsReturn.Id.ToString(), BranchId = branch.Id,
            RequestedBy = "encoder", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new ApproveGoodsReturnCommand(goodsReturn.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReturn.ExceedsReceivedQty");
        await db.DisposeAsync();
    }
}
