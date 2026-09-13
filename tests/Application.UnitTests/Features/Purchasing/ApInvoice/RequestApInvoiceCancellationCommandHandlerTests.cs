namespace ZARI.Application.UnitTests.Features.Purchasing.ApInvoice;

using ZARI.Application.Features.Purchasing.ApInvoices.RequestCancellation;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class RequestApInvoiceCancellationCommandHandlerTests
{
    private static RequestApInvoiceCancellationCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new SubmitForApprovalCommandHandler(db), new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.ApInvoice invoice)> Seed(string status = "POSTED")
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var supplier = PurchasingTestFixtures.Supplier();
        db.Suppliers.Add(supplier);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var invoice = PurchasingTestFixtures.ApInvoice(branch.Id, supplier.Id, item.Id, uom.Id, status: status);
        db.ApInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, invoice);
    }

    [Fact]
    public async Task HandleAsync_Should_Request_Cancellation_Of_Posted_Invoice()
    {
        var (db, invoice) = await Seed();

        var result = await Handler(db).HandleAsync(new RequestApInvoiceCancellationCommand(invoice.Id, "manager", "wrong amount"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("PENDING_CANCELLATION");
        db.ApprovalRequests.Should().ContainSingle(r => r.EntityType == "AP_INVOICE" && r.RequestType == "CANCEL");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new RequestApInvoiceCancellationCommand(Guid.NewGuid(), "manager", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, invoice) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("AP_INVOICES", FormAction.Cancel, invoice.BranchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new RequestApInvoiceCancellationCommand(invoice.Id, "manager", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Posted()
    {
        var (db, invoice) = await Seed(status: "DRAFT");

        var result = await Handler(db).HandleAsync(new RequestApInvoiceCancellationCommand(invoice.Id, "manager", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApInvoice.NotPosted");
        await db.DisposeAsync();
    }
}
