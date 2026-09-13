namespace ZARI.Application.UnitTests.Features.Sales.SalesInvoice;

using ZARI.Application.Features.Sales.SalesInvoices.Get;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetSalesInvoiceQueryHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.SalesInvoice invoice)> Seed()
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
        var invoice = SalesTestFixtures.SalesInvoice(branch.Id, customer.Id, item.Id, uom.Id, qty: 1, unitPrice: 100);
        db.SalesInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var payment = SalesTestFixtures.CustomerPayment(branch.Id, customer.Id, cashGlAccount.Id, invoice.Id, status: "POSTED", amount: 40);
        db.CustomerPayments.Add(payment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, invoice);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Invoice_With_Live_Balance_And_Payment_History()
    {
        var (db, invoice) = await Seed();
        var handler = new GetSalesInvoiceQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetSalesInvoiceQuery(invoice.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Lines.Should().ContainSingle();
        result.Value!.AmountPaid.Should().Be(40);
        result.Value!.Balance.Should().Be(60);
        result.Value!.Payments.Should().ContainSingle();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetSalesInvoiceQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetSalesInvoiceQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, invoice) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("SALES_INVOICES", FormAction.View, invoice.BranchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetSalesInvoiceQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetSalesInvoiceQuery(invoice.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}
