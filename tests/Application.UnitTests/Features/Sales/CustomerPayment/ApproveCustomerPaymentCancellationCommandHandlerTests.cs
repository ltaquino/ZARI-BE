namespace ZARI.Application.UnitTests.Features.Sales.CustomerPayment;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Features.Accounting.GlJournals.Reverse;
using ZARI.Application.Features.Sales.CustomerPayments.ApproveCancellation;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>Uses a real ReverseGlJournalsCommandHandler (no ExecuteUpdateAsync there) and fakes only DecideApprovalRequestCommand. Mirrors ApproveOutgoingPaymentCancellationCommandHandlerTests exactly, AR-side.</summary>
public sealed class ApproveCustomerPaymentCancellationCommandHandlerTests
{
    private static ApproveCustomerPaymentCancellationCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new ReverseGlJournalsCommandHandler(db, new GetNextDocumentNumberCommandHandler(db)),
            LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(Dummy())),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "CUSTOMER_PAYMENT", "x", "br-1", "user", DateTimeOffset.UtcNow, "APPROVED", "CANCEL", null, [], DateTimeOffset.UtcNow);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, CustomerPayment payment, SalesInvoice invoice)> Seed(
        string status = "PENDING_CANCELLATION", bool withApprovalRequest = true, bool withPostedJournal = true, decimal invoiceUnitPrice = 100, decimal paymentAmount = 100)
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
        var invoice = SalesTestFixtures.SalesInvoice(branch.Id, customer.Id, item.Id, uom.Id, status: "PAID", qty: 1, unitPrice: invoiceUnitPrice);
        db.SalesInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var payment = SalesTestFixtures.CustomerPayment(branch.Id, customer.Id, cashGlAccount.Id, invoice.Id, status: status, amount: paymentAmount);
        db.CustomerPayments.Add(payment);
        if (withPostedJournal)
        {
            db.GlJournals.Add(new GlJournal
            {
                JournalNo = "JV-0001", BranchId = branch.Id, JournalDate = DateTimeOffset.UtcNow, SourceModule = "SALES",
                SourceReferenceTable = "CustomerPayment", SourceReferenceId = payment.Id.ToString(), Status = "POSTED",
                Lines = [new GlJournalLine { AccountId = cashGlAccount.Id, DebitAmount = paymentAmount, CreditAmount = 0 }, new GlJournalLine { AccountId = arGlAccount.Id, DebitAmount = 0, CreditAmount = paymentAmount }]
            });
        }
        if (withApprovalRequest)
        {
            db.ApprovalRequests.Add(new ApprovalRequest
            {
                EntityType = "CUSTOMER_PAYMENT", EntityId = payment.Id.ToString(), BranchId = branch.Id,
                RequestedBy = "manager", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "CANCEL"
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, payment, invoice);
    }

    [Fact]
    public async Task HandleAsync_Should_Approve_Cancellation_And_Reverse_The_Posted_Journal_And_Revert_Invoice_Status()
    {
        var (db, payment, invoice) = await Seed();

        var result = await Handler(db).HandleAsync(new ApproveCustomerPaymentCancellationCommand(payment.Id, "admin.hq", "confirmed"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("CANCELLED");
        (await db.GlJournals.CountAsync(j => j.Status == "REVERSED", TestContext.Current.CancellationToken)).Should().Be(1);
        (await db.SalesInvoices.FirstAsync(i => i.Id == invoice.Id, TestContext.Current.CancellationToken)).Status.Should().Be("POSTED");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new ApproveCustomerPaymentCancellationCommand(Guid.NewGuid(), "admin.hq", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, payment, _) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasCancellationAuthorityAsync("CUSTOMER_PAYMENTS", Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new ApproveCustomerPaymentCancellationCommand(payment.Id, "admin.hq", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Cancellation()
    {
        var (db, payment, _) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(new ApproveCustomerPaymentCancellationCommand(payment.Id, "admin.hq", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CustomerPayment.NotPendingCancellation");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Cancellation_Request_Found()
    {
        var (db, payment, _) = await Seed(withApprovalRequest: false);

        var result = await Handler(db).HandleAsync(new ApproveCustomerPaymentCancellationCommand(payment.Id, "admin.hq", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApprovalRequest.NotFound");
        await db.DisposeAsync();
    }
}
