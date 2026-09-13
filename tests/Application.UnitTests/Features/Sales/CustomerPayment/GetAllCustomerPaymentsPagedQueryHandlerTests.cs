namespace ZARI.Application.UnitTests.Features.Sales.CustomerPayment;

using ZARI.Application.Features.Sales.CustomerPayments.GetAllPaged;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetAllCustomerPaymentsPagedQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_A_Page_Of_Payments()
    {
        await using var db = TestDbContextFactory.Create();
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
        for (var i = 0; i < 3; i++) db.CustomerPayments.Add(SalesTestFixtures.CustomerPayment(branch.Id, customer.Id, cashGlAccount.Id, invoice.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllCustomerPaymentsPagedQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllCustomerPaymentsPagedQuery(Page: 1, PageSize: 2), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value!.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task HandleAsync_Should_Filter_By_Search()
    {
        await using var db = TestDbContextFactory.Create();
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
        var payment = SalesTestFixtures.CustomerPayment(branch.Id, customer.Id, cashGlAccount.Id, invoice.Id);
        payment.PaymentNo = "CRV-FINDME";
        db.CustomerPayments.Add(payment);
        db.CustomerPayments.Add(SalesTestFixtures.CustomerPayment(branch.Id, customer.Id, cashGlAccount.Id, invoice.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllCustomerPaymentsPagedQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllCustomerPaymentsPagedQuery(Page: 1, PageSize: 20, Search: "FINDME"), TestContext.Current.CancellationToken);

        result.Value!.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("CUSTOMER_PAYMENTS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllCustomerPaymentsPagedQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllCustomerPaymentsPagedQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
