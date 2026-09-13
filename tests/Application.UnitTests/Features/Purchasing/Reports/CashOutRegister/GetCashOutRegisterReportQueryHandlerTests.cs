namespace ZARI.Application.UnitTests.Features.Purchasing.Reports.CashOutRegister;

using ZARI.Application.Features.Purchasing.Reports.CashOutRegister;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetCashOutRegisterReportQueryHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.Branch branch, ZARI.Domain.Entities.Supplier supplier, ZARI.Domain.Entities.BankAccount bankAccount, ZARI.Domain.Entities.ApInvoice invoice)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var supplier = PurchasingTestFixtures.Supplier();
        db.Suppliers.Add(supplier);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        var glAccount = LoanTestFixtures.GlAccount(code: "1000");
        db.GlAccounts.Add(glAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        var bankAccount = AccountingTestFixtures.BankAccount(branch.Id, glAccount.Id);
        db.BankAccounts.Add(bankAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var invoice = PurchasingTestFixtures.ApInvoice(branch.Id, supplier.Id, item.Id, uom.Id);
        db.ApInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch, supplier, bankAccount, invoice);
    }

    [Fact]
    public async Task HandleAsync_Should_Accumulate_Running_Total_For_Posted_Payments_Only()
    {
        var (db, branch, supplier, bankAccount, invoice) = await Seed();
        var posted1 = PurchasingTestFixtures.OutgoingPayment(branch.Id, supplier.Id, bankAccount.Id, invoice.Id, status: "POSTED", amount: 40);
        posted1.PaymentDate = DateTimeOffset.UtcNow.AddDays(-2);
        var draft = PurchasingTestFixtures.OutgoingPayment(branch.Id, supplier.Id, bankAccount.Id, invoice.Id, status: "DRAFT", amount: 500);
        draft.PaymentDate = DateTimeOffset.UtcNow.AddDays(-1);
        var posted2 = PurchasingTestFixtures.OutgoingPayment(branch.Id, supplier.Id, bankAccount.Id, invoice.Id, status: "POSTED", amount: 60);
        posted2.PaymentDate = DateTimeOffset.UtcNow;
        db.OutgoingPayments.AddRange(posted1, draft, posted2);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetCashOutRegisterReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetCashOutRegisterReportQuery(null, null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Rows.Should().HaveCount(3);
        result.Value!.Rows[0].RunningTotal.Should().Be(40);
        result.Value!.Rows[1].RunningTotal.Should().Be(40);
        result.Value!.Rows[2].RunningTotal.Should().Be(100);
        result.Value!.TotalPaidOut.Should().Be(100);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Filter_By_Branch_And_BankAccount()
    {
        var (db, branch, supplier, bankAccount, invoice) = await Seed();
        var otherBankAccount = AccountingTestFixtures.BankAccount(branch.Id, bankAccount.GlAccountId, accountName: "Other Account", accountNumber: "999");
        db.BankAccounts.Add(otherBankAccount);
        var otherBranch = LoanTestFixtures.Branch(id: "br-2");
        db.Branches.Add(otherBranch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.OutgoingPayments.Add(PurchasingTestFixtures.OutgoingPayment(branch.Id, supplier.Id, bankAccount.Id, invoice.Id, status: "POSTED", amount: 40));
        db.OutgoingPayments.Add(PurchasingTestFixtures.OutgoingPayment(branch.Id, supplier.Id, otherBankAccount.Id, invoice.Id, status: "POSTED", amount: 50));
        db.OutgoingPayments.Add(PurchasingTestFixtures.OutgoingPayment(otherBranch.Id, supplier.Id, bankAccount.Id, invoice.Id, status: "POSTED", amount: 60));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetCashOutRegisterReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetCashOutRegisterReportQuery(branch.Id, bankAccount.Id), TestContext.Current.CancellationToken);

        result.Value!.Rows.Should().ContainSingle();
        result.Value!.TotalPaidOut.Should().Be(40);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("OUTGOING_PAYMENTS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetCashOutRegisterReportQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetCashOutRegisterReportQuery(null, null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
