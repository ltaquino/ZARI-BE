namespace ZARI.Application.UnitTests.Features.Sales.SalesInvoice;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Features.Accounting.GlJournals.Reverse;
using ZARI.Application.Features.Sales.SalesInvoices.ApproveCancellation;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>Uses a real ReverseGlJournalsCommandHandler (no ExecuteUpdateAsync there) and fakes only DecideApprovalRequestCommand.</summary>
public sealed class ApproveSalesInvoiceCancellationCommandHandlerTests
{
    private static ApproveSalesInvoiceCancellationCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new ReverseGlJournalsCommandHandler(db, new GetNextDocumentNumberCommandHandler(db)),
            LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(Dummy())),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "SALES_INVOICE", "x", "br-1", "user", DateTimeOffset.UtcNow, "APPROVED", "CANCEL", null, [], DateTimeOffset.UtcNow);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, SalesInvoice invoice)> Seed(
        string status = "PENDING_CANCELLATION", bool withApprovalRequest = true, bool withPostedJournal = true)
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var customer = LoanTestFixtures.Customer(branch.Id);
        db.Customers.Add(customer);
        var arGlAccount = LoanTestFixtures.GlAccount(code: "1200", name: "Accounts Receivable", accountType: "Asset", normalBalance: "Debit");
        var revenueGlAccount = LoanTestFixtures.GlAccount(code: "4000", name: "Sales Revenue", accountType: "Revenue", normalBalance: "Credit");
        db.GlAccounts.AddRange(arGlAccount, revenueGlAccount);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var invoice = SalesTestFixtures.SalesInvoice(branch.Id, customer.Id, item.Id, uom.Id, status: status, qty: 1, unitPrice: 100);
        invoice.BirOrSeriesNumber = "BIR-OR-00001";
        db.SalesInvoices.Add(invoice);
        if (withPostedJournal)
        {
            db.GlJournals.Add(new GlJournal
            {
                JournalNo = "JV-0001", BranchId = branch.Id, JournalDate = DateTimeOffset.UtcNow, SourceModule = "SALES",
                SourceReferenceTable = "SalesInvoice", SourceReferenceId = invoice.Id.ToString(), Status = "POSTED",
                Lines = [new GlJournalLine { AccountId = arGlAccount.Id, DebitAmount = 100, CreditAmount = 0 }, new GlJournalLine { AccountId = revenueGlAccount.Id, DebitAmount = 0, CreditAmount = 100 }]
            });
        }
        if (withApprovalRequest)
        {
            db.ApprovalRequests.Add(new ApprovalRequest
            {
                EntityType = "SALES_INVOICE", EntityId = invoice.Id.ToString(), BranchId = branch.Id,
                RequestedBy = "manager", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "CANCEL"
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, invoice);
    }

    [Fact]
    public async Task HandleAsync_Should_Approve_Cancellation_And_Reverse_The_Posted_Journal_Keeping_BirOr_Number()
    {
        var (db, invoice) = await Seed();

        var result = await Handler(db).HandleAsync(new ApproveSalesInvoiceCancellationCommand(invoice.Id, "admin.hq", "confirmed"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("CANCELLED");
        result.Value!.BirOrSeriesNumber.Should().Be("BIR-OR-00001");
        (await db.GlJournals.CountAsync(j => j.Status == "REVERSED", TestContext.Current.CancellationToken)).Should().Be(1);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new ApproveSalesInvoiceCancellationCommand(Guid.NewGuid(), "admin.hq", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, invoice) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasCancellationAuthorityAsync("SALES_INVOICES", Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new ApproveSalesInvoiceCancellationCommand(invoice.Id, "admin.hq", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Cancellation()
    {
        var (db, invoice) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(new ApproveSalesInvoiceCancellationCommand(invoice.Id, "admin.hq", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("SalesInvoice.NotPendingCancellation");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Cancellation_Request_Found()
    {
        var (db, invoice) = await Seed(withApprovalRequest: false);

        var result = await Handler(db).HandleAsync(new ApproveSalesInvoiceCancellationCommand(invoice.Id, "admin.hq", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApprovalRequest.NotFound");
        await db.DisposeAsync();
    }
}
