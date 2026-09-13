namespace ZARI.Application.UnitTests.Features.Purchasing.OutgoingPayment;

using ZARI.Application.Features.Purchasing.OutgoingPayments.GetAllPaged;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetAllOutgoingPaymentsPagedQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_A_Page_Of_Payments()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var supplier = PurchasingTestFixtures.Supplier();
        db.Suppliers.Add(supplier);
        var bankGlAccount = LoanTestFixtures.GlAccount(code: "1010");
        db.GlAccounts.Add(bankGlAccount);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        var bankAccount = AccountingTestFixtures.BankAccount(branch.Id, bankGlAccount.Id);
        db.BankAccounts.Add(bankAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var invoices = new List<ZARI.Domain.Entities.ApInvoice>();
        for (var i = 0; i < 3; i++)
        {
            var invoice = PurchasingTestFixtures.ApInvoice(branch.Id, supplier.Id, item.Id, uom.Id);
            invoices.Add(invoice);
            db.ApInvoices.Add(invoice);
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        foreach (var invoice in invoices)
            db.OutgoingPayments.Add(PurchasingTestFixtures.OutgoingPayment(branch.Id, supplier.Id, bankAccount.Id, invoice.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllOutgoingPaymentsPagedQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllOutgoingPaymentsPagedQuery(Page: 1, PageSize: 2), TestContext.Current.CancellationToken);

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
        var supplier = PurchasingTestFixtures.Supplier();
        db.Suppliers.Add(supplier);
        var bankGlAccount = LoanTestFixtures.GlAccount(code: "1010");
        db.GlAccounts.Add(bankGlAccount);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        var bankAccount = AccountingTestFixtures.BankAccount(branch.Id, bankGlAccount.Id);
        db.BankAccounts.Add(bankAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var invoice1 = PurchasingTestFixtures.ApInvoice(branch.Id, supplier.Id, item.Id, uom.Id);
        var invoice2 = PurchasingTestFixtures.ApInvoice(branch.Id, supplier.Id, item.Id, uom.Id);
        db.ApInvoices.AddRange(invoice1, invoice2);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var payment = PurchasingTestFixtures.OutgoingPayment(branch.Id, supplier.Id, bankAccount.Id, invoice1.Id);
        payment.PaymentNo = "OP-FINDME";
        db.OutgoingPayments.Add(payment);
        db.OutgoingPayments.Add(PurchasingTestFixtures.OutgoingPayment(branch.Id, supplier.Id, bankAccount.Id, invoice2.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllOutgoingPaymentsPagedQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllOutgoingPaymentsPagedQuery(Page: 1, PageSize: 20, Search: "FINDME"), TestContext.Current.CancellationToken);

        result.Value!.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("OUTGOING_PAYMENTS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllOutgoingPaymentsPagedQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllOutgoingPaymentsPagedQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
