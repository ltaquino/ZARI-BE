namespace ZARI.Application.UnitTests.Features.Sales.SalesInvoice;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Sales.SalesInvoices.Approve;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Nothing here runs its own transaction (no stock engine involved), so a plain SaveChangesAsync
/// is enough — faking only DecideApprovalRequestCommand unlocks full success-path coverage,
/// including a real PostGlJournalCommandHandler + GetNextDocumentNumberCommandHandler actually
/// assigning a BIR-OR number and posting a GlJournal.
/// </summary>
public sealed class ApproveSalesInvoiceCommandHandlerTests
{
    private static ApproveSalesInvoiceCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(Dummy())),
            new GetNextDocumentNumberCommandHandler(db), new PostGlJournalCommandHandler(db, new GetNextDocumentNumberCommandHandler(db)),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "SALES_INVOICE", "x", "br-1", "user", DateTimeOffset.UtcNow, "APPROVED", "SUBMIT", null, [], DateTimeOffset.UtcNow);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, SalesInvoice invoice)> Seed(string status = "PENDING_APPROVAL", bool withApprovalRequest = true)
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var customer = LoanTestFixtures.Customer(branch.Id);
        db.Customers.Add(customer);
        var arGlAccount = LoanTestFixtures.GlAccount(code: "1200", name: "Accounts Receivable", accountType: "Asset", normalBalance: "Debit");
        var revenueGlAccount = LoanTestFixtures.GlAccount(code: "4000", name: "Sales Revenue", accountType: "Revenue", normalBalance: "Credit");
        var vatGlAccount = LoanTestFixtures.GlAccount(code: "2200", name: "VAT Payable", accountType: "Liability", normalBalance: "Credit");
        db.GlAccounts.AddRange(arGlAccount, revenueGlAccount, vatGlAccount);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var invoice = SalesTestFixtures.SalesInvoice(branch.Id, customer.Id, item.Id, uom.Id, status: status, qty: 1, unitPrice: 112);
        db.SalesInvoices.Add(invoice);
        if (withApprovalRequest)
        {
            db.ApprovalRequests.Add(new ApprovalRequest
            {
                EntityType = "SALES_INVOICE", EntityId = invoice.Id.ToString(), BranchId = branch.Id,
                RequestedBy = "encoder", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT"
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, invoice);
    }

    [Fact]
    public async Task HandleAsync_Should_Approve_Assign_BirOr_Number_And_Post_A_Real_GlJournal()
    {
        var (db, invoice) = await Seed();

        var result = await Handler(db).HandleAsync(new ApproveSalesInvoiceCommand(invoice.Id, "manager", "ok"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("POSTED");
        result.Value!.BirOrSeriesNumber.Should().NotBeNullOrEmpty();
        db.GlJournals.Should().ContainSingle(j => j.SourceReferenceTable == "SalesInvoice" && j.SourceReferenceId == invoice.Id.ToString());
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new ApproveSalesInvoiceCommand(Guid.NewGuid(), "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, invoice) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("SALES_INVOICES", FormAction.Approve, invoice.BranchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new ApproveSalesInvoiceCommand(invoice.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Approval()
    {
        var (db, invoice) = await Seed(status: "DRAFT");

        var result = await Handler(db).HandleAsync(new ApproveSalesInvoiceCommand(invoice.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("SalesInvoice.NotPendingApproval");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Approval_Request_Found()
    {
        var (db, invoice) = await Seed(withApprovalRequest: false);

        var result = await Handler(db).HandleAsync(new ApproveSalesInvoiceCommand(invoice.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApprovalRequest.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Exceeds_Delivered_Qty_On_Reapproval_Race()
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
        var doOrder = SalesTestFixtures.DeliveryOrder(branch.Id, warehouse.Id, customer.Id, item.Id, uom.Id, status: "POSTED", qty: 10);
        db.DeliveryOrders.Add(doOrder);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Another invoice already claimed 8 of the 10 delivered.
        var otherInvoice = SalesTestFixtures.SalesInvoice(branch.Id, customer.Id, item.Id, uom.Id, status: "POSTED", qty: 8, deliveryOrderId: doOrder.Id, deliveryOrderLineId: doOrder.Lines[0].Id);
        db.SalesInvoices.Add(otherInvoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // This invoice (pending approval) claims 5 more — 8 + 5 > 10.
        var invoice = SalesTestFixtures.SalesInvoice(branch.Id, customer.Id, item.Id, uom.Id, status: "PENDING_APPROVAL", qty: 5, deliveryOrderId: doOrder.Id, deliveryOrderLineId: doOrder.Lines[0].Id);
        db.SalesInvoices.Add(invoice);
        db.ApprovalRequests.Add(new ApprovalRequest
        {
            EntityType = "SALES_INVOICE", EntityId = invoice.Id.ToString(), BranchId = branch.Id,
            RequestedBy = "encoder", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new ApproveSalesInvoiceCommand(invoice.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("SalesInvoice.ExceedsDeliveredQty");
        await db.DisposeAsync();
    }
}
