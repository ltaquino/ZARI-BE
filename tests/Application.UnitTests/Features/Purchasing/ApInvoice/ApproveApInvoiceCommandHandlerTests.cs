namespace ZARI.Application.UnitTests.Features.Purchasing.ApInvoice;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Purchasing.ApInvoices.Approve;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Unlike GRPO/GoodsReturn, an AP invoice never touches stock — its own status flip is a plain
/// tracked-entity SaveChangesAsync, so faking only DecideApprovalRequestCommand unlocks full
/// success-path coverage, including a real PostGlJournalCommandHandler actually posting a GlJournal
/// (both the ITEM/GRNI-clearing and EXPENSE journal shapes).
/// </summary>
public sealed class ApproveApInvoiceCommandHandlerTests
{
    private static ApproveApInvoiceCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(Dummy())),
            new PostGlJournalCommandHandler(db, new GetNextDocumentNumberCommandHandler(db)),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "AP_INVOICE", "x", "br-1", "user", DateTimeOffset.UtcNow, "APPROVED", "SUBMIT", null, [], DateTimeOffset.UtcNow);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, string branchId, Guid supplierId, Guid warehouseId, Guid itemId, Guid uomId)> SeedBase()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        var supplier = PurchasingTestFixtures.Supplier();
        db.Suppliers.Add(supplier);
        var grniAccount = LoanTestFixtures.GlAccount(code: "2100", name: "Goods Received Not Invoiced", accountType: "Liability", normalBalance: "Credit");
        var apAccount = LoanTestFixtures.GlAccount(code: "2000", name: "Accounts Payable", accountType: "Liability", normalBalance: "Credit");
        var ppvAccount = LoanTestFixtures.GlAccount(code: "5200", name: "Purchase Price Variance", accountType: "Expense", normalBalance: "Debit");
        db.GlAccounts.AddRange(grniAccount, apAccount, ppvAccount);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch.Id, supplier.Id, warehouse.Id, item.Id, uom.Id);
    }

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.ApInvoice invoice, GoodsReceiptPo grpo)> SeedItemInvoice(
        decimal receivedUnitCost = 100, decimal invoicedUnitCost = 100, string status = "PENDING_APPROVAL", bool withApprovalRequest = true)
    {
        var (db, branchId, supplierId, warehouseId, itemId, uomId) = await SeedBase();
        var grpo = PurchasingTestFixtures.GoodsReceiptPo(branchId, warehouseId, supplierId, itemId, uomId, status: "POSTED", qty: 10, unitCost: receivedUnitCost);
        db.GoodsReceiptPos.Add(grpo);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var invoice = PurchasingTestFixtures.ApInvoice(branchId, supplierId, itemId, uomId, status: status, qty: 5, unitCost: invoicedUnitCost, goodsReceiptPoId: grpo.Id, goodsReceiptPoLineId: grpo.Lines[0].Id);
        db.ApInvoices.Add(invoice);
        if (withApprovalRequest)
        {
            db.ApprovalRequests.Add(new ApprovalRequest
            {
                EntityType = "AP_INVOICE", EntityId = invoice.Id.ToString(), BranchId = branchId,
                RequestedBy = "encoder", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT"
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, invoice, grpo);
    }

    [Fact]
    public async Task HandleAsync_Should_Approve_Item_Invoice_And_Post_A_Real_GlJournal()
    {
        var (db, invoice, _) = await SeedItemInvoice();

        var result = await Handler(db).HandleAsync(new ApproveApInvoiceCommand(invoice.Id, "manager", "ok"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("POSTED");
        db.GlJournals.Should().ContainSingle(j => j.SourceReferenceTable == "ApInvoice" && j.SourceReferenceId == invoice.Id.ToString());
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Post_Unfavorable_Variance_When_Invoiced_More_Than_Received()
    {
        var (db, invoice, _) = await SeedItemInvoice(receivedUnitCost: 100, invoicedUnitCost: 120);

        var result = await Handler(db).HandleAsync(new ApproveApInvoiceCommand(invoice.Id, "manager", "ok"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var journal = await db.GlJournals.Include(j => j.Lines).FirstAsync(j => j.SourceReferenceId == invoice.Id.ToString(), TestContext.Current.CancellationToken);
        journal.Lines.Should().HaveCount(3);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Approve_Expense_Invoice_And_Post_A_Real_GlJournal()
    {
        var (db, branchId, supplierId, _, _, _) = await SeedBase();
        var expenseAccount = LoanTestFixtures.GlAccount(code: "6100", name: "Utilities Expense", accountType: "Expense");
        db.GlAccounts.Add(expenseAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var invoice = PurchasingTestFixtures.ApInvoiceExpense(branchId, supplierId, expenseAccount.Id, status: "PENDING_APPROVAL");
        db.ApInvoices.Add(invoice);
        db.ApprovalRequests.Add(new ApprovalRequest
        {
            EntityType = "AP_INVOICE", EntityId = invoice.Id.ToString(), BranchId = branchId,
            RequestedBy = "encoder", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new ApproveApInvoiceCommand(invoice.Id, "manager", "ok"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("POSTED");
        db.GlJournals.Should().ContainSingle(j => j.SourceReferenceTable == "ApInvoice" && j.SourceReferenceId == invoice.Id.ToString());
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new ApproveApInvoiceCommand(Guid.NewGuid(), "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, invoice, _) = await SeedItemInvoice();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("AP_INVOICES", FormAction.Approve, invoice.BranchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new ApproveApInvoiceCommand(invoice.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Approval()
    {
        var (db, invoice, _) = await SeedItemInvoice(status: "DRAFT");

        var result = await Handler(db).HandleAsync(new ApproveApInvoiceCommand(invoice.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApInvoice.NotPendingApproval");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Approval_Request_Found()
    {
        var (db, invoice, _) = await SeedItemInvoice(withApprovalRequest: false);

        var result = await Handler(db).HandleAsync(new ApproveApInvoiceCommand(invoice.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApprovalRequest.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Exceeds_Received_Qty_On_Reapproval_Race()
    {
        var (db, invoice, grpo) = await SeedItemInvoice();
        // Another invoice already claimed 8 of the 10 received in between Create and Approve.
        var otherInvoice = PurchasingTestFixtures.ApInvoice(invoice.BranchId, invoice.SupplierId, invoice.Lines[0].ItemId, invoice.Lines[0].UomId, status: "POSTED", qty: 8, goodsReceiptPoId: grpo.Id, goodsReceiptPoLineId: grpo.Lines[0].Id);
        db.ApInvoices.Add(otherInvoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new ApproveApInvoiceCommand(invoice.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApInvoice.ExceedsReceivedQty");
        await db.DisposeAsync();
    }
}
