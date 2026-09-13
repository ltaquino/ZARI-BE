namespace ZARI.Application.UnitTests.Features.Sales.Reports;

using ZARI.Application.Features.Sales.Reports.CashReceiptsBook;
using ZARI.Application.UnitTests.TestSupport;

public sealed class GetCashReceiptsBookReportQueryHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.Branch branch, Guid customerId, Guid cashAccountId, Guid salesInvoiceId)> Seed()
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
        return (db, branch, customer.Id, cashGlAccount.Id, invoice.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branch, customerId, cashAccountId, salesInvoiceId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("CUSTOMER_PAYMENTS", ZARI.Domain.Common.FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetCashReceiptsBookReportQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetCashReceiptsBookReportQuery(null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ZARI.Domain.Common.ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Accumulate_Running_Total_For_Posted_Payments_Only()
    {
        var (db, branch, customerId, cashAccountId, salesInvoiceId) = await Seed();
        var posted1 = SalesTestFixtures.CustomerPayment(branch.Id, customerId, cashAccountId, salesInvoiceId, status: "POSTED", amount: 40);
        posted1.PaymentDate = DateTimeOffset.UtcNow.AddDays(-2);
        var draft = SalesTestFixtures.CustomerPayment(branch.Id, customerId, cashAccountId, salesInvoiceId, status: "DRAFT", amount: 500);
        draft.PaymentDate = DateTimeOffset.UtcNow.AddDays(-1);
        var posted2 = SalesTestFixtures.CustomerPayment(branch.Id, customerId, cashAccountId, salesInvoiceId, status: "POSTED", amount: 60);
        posted2.PaymentDate = DateTimeOffset.UtcNow;
        db.CustomerPayments.AddRange(posted1, draft, posted2);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetCashReceiptsBookReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetCashReceiptsBookReportQuery(null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Rows.Should().HaveCount(3);
        result.Value!.Rows[0].RunningTotal.Should().Be(40);
        result.Value!.Rows[1].RunningTotal.Should().Be(40);
        result.Value!.Rows[2].RunningTotal.Should().Be(100);
        result.Value!.TotalReceived.Should().Be(100);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Filter_By_Branch()
    {
        var (db, branch, customerId, cashAccountId, salesInvoiceId) = await Seed();
        var otherBranch = LoanTestFixtures.Branch(id: "br-2");
        db.Branches.Add(otherBranch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.CustomerPayments.Add(SalesTestFixtures.CustomerPayment(branch.Id, customerId, cashAccountId, salesInvoiceId, status: "POSTED", amount: 40));
        db.CustomerPayments.Add(SalesTestFixtures.CustomerPayment(otherBranch.Id, customerId, cashAccountId, salesInvoiceId, status: "POSTED", amount: 60));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetCashReceiptsBookReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetCashReceiptsBookReportQuery(branch.Id), TestContext.Current.CancellationToken);

        result.Value!.Rows.Should().ContainSingle();
        result.Value!.TotalReceived.Should().Be(40);
        await db.DisposeAsync();
    }
}
