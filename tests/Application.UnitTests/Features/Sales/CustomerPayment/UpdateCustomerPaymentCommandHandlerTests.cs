namespace ZARI.Application.UnitTests.Features.Sales.CustomerPayment;

using ZARI.Application.Features.Sales.CustomerPayments.Create;
using ZARI.Application.Features.Sales.CustomerPayments.Update;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class UpdateCustomerPaymentCommandHandlerTests
{
    private static UpdateCustomerPaymentCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static UpdateCustomerPaymentCommand Command(Guid id, Guid cashAccountId, Guid salesInvoiceId, decimal amount = 50) =>
        new(id, "CASH", cashAccountId, DateTimeOffset.UtcNow, "CHK-2", "updated", null, "encoder", [new CustomerPaymentLineInput(salesInvoiceId, amount)]);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, Guid cashAccountId, Guid salesInvoiceId, ZARI.Domain.Entities.CustomerPayment payment)> Seed(string status = "DRAFT")
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
        var invoice = SalesTestFixtures.SalesInvoice(branch.Id, customer.Id, item.Id, uom.Id, status: "POSTED", qty: 1, unitPrice: 100);
        db.SalesInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var payment = SalesTestFixtures.CustomerPayment(branch.Id, customer.Id, cashGlAccount.Id, invoice.Id, status: status);
        db.CustomerPayments.Add(payment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, cashGlAccount.Id, invoice.Id, payment);
    }

    [Fact]
    public async Task HandleAsync_Should_Update_Draft_Payment()
    {
        var (db, cashAccountId, salesInvoiceId, payment) = await Seed();

        var result = await Handler(db).HandleAsync(Command(payment.Id, cashAccountId, salesInvoiceId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Remarks.Should().Be("updated");
        result.Value!.Lines.Should().ContainSingle(l => l.AmountApplied == 50);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        var (db, cashAccountId, salesInvoiceId, _) = await Seed();

        var result = await Handler(db).HandleAsync(Command(Guid.NewGuid(), cashAccountId, salesInvoiceId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, cashAccountId, salesInvoiceId, payment) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("CUSTOMER_PAYMENTS", FormAction.Edit, payment.BranchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(Command(payment.Id, cashAccountId, salesInvoiceId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, cashAccountId, salesInvoiceId, payment) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(Command(payment.Id, cashAccountId, salesInvoiceId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CustomerPayment.NotDraft");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Gl_Account_Not_Found()
    {
        var (db, _, salesInvoiceId, payment) = await Seed();

        var result = await Handler(db).HandleAsync(Command(payment.Id, Guid.NewGuid(), salesInvoiceId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlAccount.NotFound");
        await db.DisposeAsync();
    }
}
