namespace ZARI.Application.UnitTests.Features.Inventory.GoodsReceipt;

using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Inventory.GoodsReceipts.Approve;
using ZARI.Application.Features.Inventory.SerialNumbers.GetAll;
using ZARI.Application.Features.Inventory.SerialNumbers.Receive;
using ZARI.Application.Features.Inventory.StockLedgers.Receive;
using ZARI.Application.Features.Inventory.StockLocationBalances.Receive;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// The real success path is not InMemory-testable: ReceiveStockCommand/ReceiveSerialCommand/
/// ReceiveIntoLocationCommand each open a real transaction (faked here so guard clauses and the
/// resume-after-partial-approve branch can be exercised), and the handler's own final status flip
/// uses ExecuteUpdateAsync (also unsupported). Every dependency is faked; only guard clauses and
/// the ApprovalRequest re-check are exercised.
/// </summary>
public sealed class ApproveGoodsReceiptCommandHandlerTests
{
    private static ApproveGoodsReceiptCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db,
            LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(Dummy())),
            LoanTestFixtures.SuccessHandler<ReceiveStockCommand, Result<ReceiveStockResponse>>(Result.Success(new ReceiveStockResponse(50))),
            LoanTestFixtures.SuccessHandler<ReceiveSerialCommand, Result<SerialNumberResponse>>(Result.Failure<SerialNumberResponse>(Error.Failure("x", "unused"))),
            LoanTestFixtures.SuccessHandler<ReceiveIntoLocationCommand, Result>(Result.Failure(Error.Failure("x", "unused"))),
            LoanTestFixtures.SuccessHandler<PostGlJournalCommand, Result<GlJournalResponse>>(Result.Failure<GlJournalResponse>(Error.Failure("x", "unused"))),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "GOODS_RECEIPT", "x", "br-1", "manager", DateTimeOffset.UtcNow, "APPROVED", "SUBMIT", null, [], DateTimeOffset.UtcNow);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, GoodsReceipt receipt, Branch branch)> Seed(string status = "PENDING_APPROVAL", bool withApprovalRequest = true, string approvalRequestStatus = "PENDING")
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var receipt = InventoryTestFixtures.GoodsReceipt(branch.Id, warehouse.Id, item.Id, uom.Id, status: status);
        db.GoodsReceipts.Add(receipt);
        if (withApprovalRequest)
        {
            db.ApprovalRequests.Add(new ApprovalRequest
            {
                EntityType = "GOODS_RECEIPT", EntityId = receipt.Id.ToString(), BranchId = branch.Id,
                RequestedBy = "encoder", RequestedAt = DateTimeOffset.UtcNow, Status = approvalRequestStatus, RequestType = "SUBMIT"
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, receipt, branch);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new ApproveGoodsReceiptCommand(Guid.NewGuid(), "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, receipt, branch) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("GOODS_RECEIPTS", FormAction.Approve, branch.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new ApproveGoodsReceiptCommand(receipt.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Approval()
    {
        var (db, receipt, _) = await Seed(status: "DRAFT");

        var result = await Handler(db).HandleAsync(new ApproveGoodsReceiptCommand(receipt.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReceipt.NotPendingApproval");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Approval_Request_Found()
    {
        var (db, receipt, _) = await Seed(withApprovalRequest: false);

        var result = await Handler(db).HandleAsync(new ApproveGoodsReceiptCommand(receipt.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApprovalRequest.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Request_Already_Decided_As_Rejected()
    {
        var (db, receipt, _) = await Seed(approvalRequestStatus: "REJECTED");

        var result = await Handler(db).HandleAsync(new ApproveGoodsReceiptCommand(receipt.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReceipt.RequestAlreadyDecided");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Resume_Into_Posting_When_Request_Already_Approved()
    {
        // A prior Approve attempt got as far as deciding (APPROVED) but failed during posting —
        // this resumes straight into the posting steps instead of re-deciding or erroring. No GL
        // account is seeded, so it should reach (and fail inside) the GL posting step rather than
        // failing at the guard clause — proving the resume branch, not the decide branch, was taken.
        var (db, receipt, _) = await Seed(approvalRequestStatus: "APPROVED");

        var result = await Handler(db).HandleAsync(new ApproveGoodsReceiptCommand(receipt.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlAccount.NotFound");
        await db.DisposeAsync();
    }
}
