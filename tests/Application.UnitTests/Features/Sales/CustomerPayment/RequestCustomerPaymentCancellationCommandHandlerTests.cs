namespace ZARI.Application.UnitTests.Features.Sales.CustomerPayment;

using ZARI.Application.Features.Sales.CustomerPayments.RequestCancellation;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class RequestCustomerPaymentCancellationCommandHandlerTests
{
    private static RequestCustomerPaymentCancellationCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new SubmitForApprovalCommandHandler(db), new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.CustomerPayment payment)> Seed(string status = "POSTED")
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var customer = LoanTestFixtures.Customer(branch.Id);
        db.Customers.Add(customer);
        var cashGlAccount = LoanTestFixtures.GlAccount(code: "1000");
        db.GlAccounts.Add(cashGlAccount);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var invoice = SalesTestFixtures.SalesInvoice(branch.Id, customer.Id, item.Id, uom.Id);
        db.SalesInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var payment = SalesTestFixtures.CustomerPayment(branch.Id, customer.Id, cashGlAccount.Id, invoice.Id, status: status);
        db.CustomerPayments.Add(payment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, payment);
    }

    [Fact]
    public async Task HandleAsync_Should_Request_Cancellation_Of_Posted_Payment()
    {
        var (db, payment) = await Seed();

        var result = await Handler(db).HandleAsync(new RequestCustomerPaymentCancellationCommand(payment.Id, "manager", "wrong amount"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("PENDING_CANCELLATION");
        db.ApprovalRequests.Should().ContainSingle(r => r.EntityType == "CUSTOMER_PAYMENT" && r.RequestType == "CANCEL");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new RequestCustomerPaymentCancellationCommand(Guid.NewGuid(), "manager", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, payment) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("CUSTOMER_PAYMENTS", FormAction.Cancel, payment.BranchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new RequestCustomerPaymentCancellationCommand(payment.Id, "manager", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Posted()
    {
        var (db, payment) = await Seed(status: "DRAFT");

        var result = await Handler(db).HandleAsync(new RequestCustomerPaymentCancellationCommand(payment.Id, "manager", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CustomerPayment.NotPosted");
        await db.DisposeAsync();
    }
}
