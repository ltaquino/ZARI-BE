namespace ZARI.Application.UnitTests.Features.Sales.CustomerPayment;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Sales.CustomerPayments.Approve;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Unlike DeliveryOrder/SalesReturn's Approve handlers, this one never touches stock — its status
/// flip is a plain tracked-entity SaveChangesAsync, so faking only DecideApprovalRequestCommand
/// unlocks full success-path coverage, including a real PostGlJournalCommandHandler actually
/// posting a GlJournal. Mirrors ApproveOutgoingPaymentCommandHandlerTests exactly, AR-side.
/// </summary>
public sealed class ApproveCustomerPaymentCommandHandlerTests
{
    private static ApproveCustomerPaymentCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(Dummy())),
            new PostGlJournalCommandHandler(db, new GetNextDocumentNumberCommandHandler(db)),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "CUSTOMER_PAYMENT", "x", "br-1", "user", DateTimeOffset.UtcNow, "APPROVED", "SUBMIT", null, [], DateTimeOffset.UtcNow);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, CustomerPayment payment, SalesInvoice invoice)> Seed(
        string paymentStatus = "PENDING_APPROVAL", string invoiceStatus = "POSTED", decimal invoiceQty = 1, decimal invoiceUnitPrice = 100, decimal paymentAmount = 100, bool withApprovalRequest = true)
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var customer = LoanTestFixtures.Customer(branch.Id);
        db.Customers.Add(customer);
        var cashGlAccount = LoanTestFixtures.GlAccount(code: "1000");
        var arGlAccount = LoanTestFixtures.GlAccount(code: "1200", name: "Accounts Receivable", accountType: "Asset", normalBalance: "Debit");
        db.GlAccounts.AddRange(cashGlAccount, arGlAccount);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var invoice = SalesTestFixtures.SalesInvoice(branch.Id, customer.Id, item.Id, uom.Id, status: invoiceStatus, qty: invoiceQty, unitPrice: invoiceUnitPrice);
        db.SalesInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var payment = SalesTestFixtures.CustomerPayment(branch.Id, customer.Id, cashGlAccount.Id, invoice.Id, status: paymentStatus, amount: paymentAmount);
        db.CustomerPayments.Add(payment);
        if (withApprovalRequest)
        {
            db.ApprovalRequests.Add(new ApprovalRequest
            {
                EntityType = "CUSTOMER_PAYMENT", EntityId = payment.Id.ToString(), BranchId = branch.Id,
                RequestedBy = "encoder", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT"
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, payment, invoice);
    }

    [Fact]
    public async Task HandleAsync_Should_Approve_And_Post_A_Real_GlJournal_And_Fully_Pay_Invoice()
    {
        var (db, payment, invoice) = await Seed(invoiceQty: 1, invoiceUnitPrice: 100, paymentAmount: 100);

        var result = await Handler(db).HandleAsync(new ApproveCustomerPaymentCommand(payment.Id, "manager", "ok"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("POSTED");
        db.GlJournals.Should().ContainSingle(j => j.SourceReferenceTable == "CustomerPayment" && j.SourceReferenceId == payment.Id.ToString());
        (await db.SalesInvoices.FirstAsync(i => i.Id == invoice.Id, TestContext.Current.CancellationToken)).Status.Should().Be("PAID");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Approve_And_Partially_Pay_Invoice()
    {
        var (db, payment, invoice) = await Seed(invoiceQty: 1, invoiceUnitPrice: 100, paymentAmount: 40);

        var result = await Handler(db).HandleAsync(new ApproveCustomerPaymentCommand(payment.Id, "manager", "ok"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.SalesInvoices.FirstAsync(i => i.Id == invoice.Id, TestContext.Current.CancellationToken)).Status.Should().Be("PARTIALLY_PAID");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new ApproveCustomerPaymentCommand(Guid.NewGuid(), "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, payment, _) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("CUSTOMER_PAYMENTS", FormAction.Approve, payment.BranchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new ApproveCustomerPaymentCommand(payment.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Approval()
    {
        var (db, payment, _) = await Seed(paymentStatus: "DRAFT");

        var result = await Handler(db).HandleAsync(new ApproveCustomerPaymentCommand(payment.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CustomerPayment.NotPendingApproval");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Approval_Request_Found()
    {
        var (db, payment, _) = await Seed(withApprovalRequest: false);

        var result = await Handler(db).HandleAsync(new ApproveCustomerPaymentCommand(payment.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApprovalRequest.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Invoice_No_Longer_Payable()
    {
        var (db, payment, _) = await Seed(invoiceStatus: "CANCELLED");

        var result = await Handler(db).HandleAsync(new ApproveCustomerPaymentCommand(payment.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CustomerPayment.InvoiceNotPayable");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Amount_Exceeds_Balance_On_Reapproval_Race()
    {
        var (db, payment, invoice) = await Seed(invoiceQty: 1, invoiceUnitPrice: 100, paymentAmount: 80);
        // Another payment already posted against this invoice in between Create and Approve.
        var customer = await db.Customers.FirstAsync(TestContext.Current.CancellationToken);
        var cashGlAccount = await db.GlAccounts.FirstAsync(a => a.Code == "1000", TestContext.Current.CancellationToken);
        var otherPayment = SalesTestFixtures.CustomerPayment(payment.BranchId, customer.Id, cashGlAccount.Id, invoice.Id, status: "POSTED", amount: 50);
        db.CustomerPayments.Add(otherPayment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new ApproveCustomerPaymentCommand(payment.Id, "manager", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CustomerPayment.AmountExceedsBalance");
        await db.DisposeAsync();
    }
}
