namespace ZARI.Application.UnitTests.Features.Sales.CustomerPayment;

using ZARI.Application.Features.Sales.CustomerPayments.RejectCancellation;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class RejectCustomerPaymentCancellationCommandHandlerTests
{
    private static RejectCustomerPaymentCancellationCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(Dummy())),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "CUSTOMER_PAYMENT", "x", "br-1", "user", DateTimeOffset.UtcNow, "REJECTED", "CANCEL", null, [], DateTimeOffset.UtcNow);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, CustomerPayment payment)> Seed(string status = "PENDING_CANCELLATION", bool withApprovalRequest = true)
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
        payment.CancelReason = "pending review";
        db.CustomerPayments.Add(payment);
        if (withApprovalRequest)
        {
            db.ApprovalRequests.Add(new ApprovalRequest
            {
                EntityType = "CUSTOMER_PAYMENT", EntityId = payment.Id.ToString(), BranchId = branch.Id,
                RequestedBy = "manager", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "CANCEL"
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, payment);
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_Cancellation_Back_To_Posted()
    {
        var (db, payment) = await Seed();

        var result = await Handler(db).HandleAsync(new RejectCustomerPaymentCancellationCommand(payment.Id, "admin.hq", "not warranted"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("POSTED");
        result.Value!.CancelReason.Should().BeNull();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new RejectCustomerPaymentCancellationCommand(Guid.NewGuid(), "admin.hq", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, payment) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasCancellationAuthorityAsync("CUSTOMER_PAYMENTS", Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new RejectCustomerPaymentCancellationCommand(payment.Id, "admin.hq", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Cancellation()
    {
        var (db, payment) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(new RejectCustomerPaymentCancellationCommand(payment.Id, "admin.hq", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CustomerPayment.NotPendingCancellation");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Cancellation_Request_Found()
    {
        var (db, payment) = await Seed(withApprovalRequest: false);

        var result = await Handler(db).HandleAsync(new RejectCustomerPaymentCancellationCommand(payment.Id, "admin.hq", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApprovalRequest.NotFound");
        await db.DisposeAsync();
    }
}
