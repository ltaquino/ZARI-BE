namespace ZARI.Application.UnitTests.Features.Purchasing.OutgoingPayment;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Purchasing.OutgoingPayments.Approve;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Unlike GoodsReceiptPo/GoodsReturn's Approve handlers, this one never touches stock — its own
/// status flip is a plain tracked-entity SaveChangesAsync, so faking only DecideApprovalRequestCommand
/// (as every other Approve handler's tests do) unlocks full success-path coverage, including a real
/// PostGlJournalCommandHandler actually posting a GlJournal.
/// </summary>
public sealed class ApproveOutgoingPaymentCommandHandlerTests
{
    private static ApproveOutgoingPaymentCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(Dummy())),
            new PostGlJournalCommandHandler(db, new GetNextDocumentNumberCommandHandler(db)),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "OUTGOING_PAYMENT", "x", "br-1", "user", DateTimeOffset.UtcNow, "APPROVED", "SUBMIT", null, [], DateTimeOffset.UtcNow);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.OutgoingPayment payment, ApInvoice invoice)> Seed(
        string paymentStatus = "PENDING_APPROVAL", string invoiceStatus = "POSTED", decimal invoiceQty = 1, decimal invoiceUnitCost = 100, decimal paymentAmount = 100, bool withApprovalRequest = true)
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var supplier = PurchasingTestFixtures.Supplier();
        db.Suppliers.Add(supplier);
        var bankGlAccount = LoanTestFixtures.GlAccount(code: "1010");
        var apGlAccount = LoanTestFixtures.GlAccount(code: "2000", name: "Accounts Payable", accountType: "Liability", normalBalance: "Credit");
        db.GlAccounts.AddRange(bankGlAccount, apGlAccount);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        var bankAccount = AccountingTestFixtures.BankAccount(branch.Id, bankGlAccount.Id);
        db.BankAccounts.Add(bankAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var invoice = PurchasingTestFixtures.ApInvoice(branch.Id, supplier.Id, item.Id, uom.Id, status: invoiceStatus, qty: invoiceQty, unitCost: invoiceUnitCost);
        db.ApInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var payment = PurchasingTestFixtures.OutgoingPayment(branch.Id, supplier.Id, bankAccount.Id, invoice.Id, status: paymentStatus, amount: paymentAmount);
        db.OutgoingPayments.Add(payment);
        if (withApprovalRequest)
        {
            db.ApprovalRequests.Add(new ApprovalRequest
            {
                EntityType = "OUTGOING_PAYMENT", EntityId = payment.Id.ToString(), BranchId = branch.Id,
                RequestedBy = "encoder", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT"
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, payment, invoice);
    }

    [Fact]
    public async Task HandleAsync_Should_Approve_And_Post_A_Real_GlJournal_And_Fully_Pay_Invoice()
    {
        var (db, payment, invoice) = await Seed(invoiceQty: 1, invoiceUnitCost: 100, paymentAmount: 100);

        var result = await Handler(db).HandleAsync(new ApproveOutgoingPaymentCommand(payment.Id, "manager", "ok"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("POSTED");
        db.GlJournals.Should().ContainSingle(j => j.SourceReferenceTable == "OutgoingPayment" && j.SourceReferenceId == payment.Id.ToString());
        (await db.ApInvoices.FirstAsync(i => i.Id == invoice.Id, TestContext.Current.CancellationToken)).Status.Should().Be("PAID");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Approve_And_Partially_Pay_Invoice()
    {
        var (db, payment, invoice) = await Seed(invoiceQty: 1, invoiceUnitCost: 100, paymentAmount: 40);

        var result = await Handler(db).HandleAsync(new ApproveOutgoingPaymentCommand(payment.Id, "manager", "ok"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.ApInvoices.FirstAsync(i => i.Id == invoice.Id, TestContext.Current.CancellationToken)).Status.Should().Be("PARTIALLY_PAID");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new ApproveOutgoingPaymentCommand(Guid.NewGuid(), "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, payment, _) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("OUTGOING_PAYMENTS", FormAction.Approve, payment.BranchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new ApproveOutgoingPaymentCommand(payment.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Approval()
    {
        var (db, payment, _) = await Seed(paymentStatus: "DRAFT");

        var result = await Handler(db).HandleAsync(new ApproveOutgoingPaymentCommand(payment.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("OutgoingPayment.NotPendingApproval");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Approval_Request_Found()
    {
        var (db, payment, _) = await Seed(withApprovalRequest: false);

        var result = await Handler(db).HandleAsync(new ApproveOutgoingPaymentCommand(payment.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApprovalRequest.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Invoice_No_Longer_Payable()
    {
        var (db, payment, _) = await Seed(invoiceStatus: "CANCELLED");

        var result = await Handler(db).HandleAsync(new ApproveOutgoingPaymentCommand(payment.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("OutgoingPayment.InvoiceNotPayable");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Amount_Exceeds_Balance_On_Reapproval_Race()
    {
        var (db, payment, invoice) = await Seed(invoiceQty: 1, invoiceUnitCost: 100, paymentAmount: 80);
        // Another payment already posted against this invoice in between Create and Approve.
        var supplier = await db.Suppliers.FirstAsync(TestContext.Current.CancellationToken);
        var bankAccount = await db.BankAccounts.FirstAsync(TestContext.Current.CancellationToken);
        var otherPayment = PurchasingTestFixtures.OutgoingPayment(payment.BranchId, supplier.Id, bankAccount.Id, invoice.Id, status: "POSTED", amount: 50);
        db.OutgoingPayments.Add(otherPayment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new ApproveOutgoingPaymentCommand(payment.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("OutgoingPayment.AmountExceedsBalance");
        await db.DisposeAsync();
    }
}
