namespace ZARI.Application.UnitTests.Features.Purchasing.ApInvoice;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Features.Accounting.GlJournals.Reverse;
using ZARI.Application.Features.Purchasing.ApInvoices.ApproveCancellation;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>Uses a real ReverseGlJournalsCommandHandler (no ExecuteUpdateAsync there) and fakes only DecideApprovalRequestCommand.</summary>
public sealed class ApproveApInvoiceCancellationCommandHandlerTests
{
    private static ApproveApInvoiceCancellationCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new ReverseGlJournalsCommandHandler(db, new GetNextDocumentNumberCommandHandler(db)),
            LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(Dummy())),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "AP_INVOICE", "x", "br-1", "user", DateTimeOffset.UtcNow, "APPROVED", "CANCEL", null, [], DateTimeOffset.UtcNow);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.ApInvoice invoice)> Seed(
        string status = "PENDING_CANCELLATION", bool withApprovalRequest = true, bool withPostedJournal = true)
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var supplier = PurchasingTestFixtures.Supplier();
        db.Suppliers.Add(supplier);
        var apAccount = LoanTestFixtures.GlAccount(code: "2000", name: "Accounts Payable", accountType: "Liability", normalBalance: "Credit");
        var grniAccount = LoanTestFixtures.GlAccount(code: "2100", name: "Goods Received Not Invoiced", accountType: "Liability", normalBalance: "Credit");
        db.GlAccounts.AddRange(apAccount, grniAccount);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var invoice = PurchasingTestFixtures.ApInvoice(branch.Id, supplier.Id, item.Id, uom.Id, status: status);
        db.ApInvoices.Add(invoice);
        if (withPostedJournal)
        {
            db.GlJournals.Add(new GlJournal
            {
                JournalNo = "JV-0001", BranchId = branch.Id, JournalDate = DateTimeOffset.UtcNow, SourceModule = "PURCHASING",
                SourceReferenceTable = "ApInvoice", SourceReferenceId = invoice.Id.ToString(), Status = "POSTED",
                Lines = [new GlJournalLine { AccountId = grniAccount.Id, DebitAmount = 100, CreditAmount = 0 }, new GlJournalLine { AccountId = apAccount.Id, DebitAmount = 0, CreditAmount = 100 }]
            });
        }
        if (withApprovalRequest)
        {
            db.ApprovalRequests.Add(new ApprovalRequest
            {
                EntityType = "AP_INVOICE", EntityId = invoice.Id.ToString(), BranchId = branch.Id,
                RequestedBy = "manager", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "CANCEL"
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, invoice);
    }

    [Fact]
    public async Task HandleAsync_Should_Approve_Cancellation_And_Reverse_The_Posted_Journal()
    {
        var (db, invoice) = await Seed();

        var result = await Handler(db).HandleAsync(new ApproveApInvoiceCancellationCommand(invoice.Id, "admin.hq", "confirmed"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("CANCELLED");
        (await db.GlJournals.CountAsync(j => j.Status == "REVERSED", TestContext.Current.CancellationToken)).Should().Be(1);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new ApproveApInvoiceCancellationCommand(Guid.NewGuid(), "admin.hq", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, invoice) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasCancellationAuthorityAsync("AP_INVOICES", Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new ApproveApInvoiceCancellationCommand(invoice.Id, "admin.hq", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Cancellation()
    {
        var (db, invoice) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(new ApproveApInvoiceCancellationCommand(invoice.Id, "admin.hq", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApInvoice.NotPendingCancellation");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Cancellation_Request_Found()
    {
        var (db, invoice) = await Seed(withApprovalRequest: false);

        var result = await Handler(db).HandleAsync(new ApproveApInvoiceCancellationCommand(invoice.Id, "admin.hq", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApprovalRequest.NotFound");
        await db.DisposeAsync();
    }
}
