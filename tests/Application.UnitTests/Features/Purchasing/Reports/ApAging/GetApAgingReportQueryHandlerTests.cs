namespace ZARI.Application.UnitTests.Features.Purchasing.Reports.ApAging;

using ZARI.Application.Features.Purchasing.Reports.ApAging;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetApAgingReportQueryHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.Branch branch, ZARI.Domain.Entities.Supplier supplier, ZARI.Domain.Entities.Item item, ZARI.Domain.Entities.Uom uom)> Seed()
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
        return (db, branch, supplier, item, uom);
    }

    [Fact]
    public async Task HandleAsync_Should_Age_Outstanding_Balance_Not_Gross_Total()
    {
        var (db, branch, supplier, item, uom) = await Seed();
        var invoice = PurchasingTestFixtures.ApInvoice(branch.Id, supplier.Id, item.Id, uom.Id, qty: 1, unitCost: 100);
        invoice.DueDate = DateTimeOffset.UtcNow.AddDays(-10);
        db.ApInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var glAccount = LoanTestFixtures.GlAccount(code: "1000");
        db.GlAccounts.Add(glAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var bankAccount = AccountingTestFixtures.BankAccount(branch.Id, glAccount.Id);
        db.BankAccounts.Add(bankAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var payment = PurchasingTestFixtures.OutgoingPayment(branch.Id, supplier.Id, bankAccount.Id, invoice.Id, status: "POSTED", amount: 40);
        db.OutgoingPayments.Add(payment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetApAgingReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetApAgingReportQuery(null, null, null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value!.Groups.Single().Invoices.Single();
        row.Outstanding.Should().Be(60);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Exclude_Fully_Paid_Invoices()
    {
        var (db, branch, supplier, item, uom) = await Seed();
        var invoice = PurchasingTestFixtures.ApInvoice(branch.Id, supplier.Id, item.Id, uom.Id, qty: 1, unitCost: 100);
        db.ApInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var glAccount = LoanTestFixtures.GlAccount(code: "1000");
        db.GlAccounts.Add(glAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var bankAccount = AccountingTestFixtures.BankAccount(branch.Id, glAccount.Id);
        db.BankAccounts.Add(bankAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.OutgoingPayments.Add(PurchasingTestFixtures.OutgoingPayment(branch.Id, supplier.Id, bankAccount.Id, invoice.Id, status: "POSTED", amount: 100));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetApAgingReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetApAgingReportQuery(null, null, null), TestContext.Current.CancellationToken);

        result.Value!.Groups.Should().BeEmpty();
        result.Value!.TotalOutstanding.Should().Be(0);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Ignore_Draft_Invoices()
    {
        var (db, branch, supplier, item, uom) = await Seed();
        db.ApInvoices.Add(PurchasingTestFixtures.ApInvoice(branch.Id, supplier.Id, item.Id, uom.Id, status: "DRAFT"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetApAgingReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetApAgingReportQuery(null, null, null), TestContext.Current.CancellationToken);

        result.Value!.Groups.Should().BeEmpty();
        await db.DisposeAsync();
    }

    [Theory]
    [InlineData(-5, "current")]
    [InlineData(10, "1-30")]
    [InlineData(45, "31-60")]
    [InlineData(75, "61-90")]
    [InlineData(120, "90+")]
    public async Task HandleAsync_Should_Bucket_By_Days_Overdue(int daysPastDue, string expectedBucket)
    {
        var (db, branch, supplier, item, uom) = await Seed();
        var invoice = PurchasingTestFixtures.ApInvoice(branch.Id, supplier.Id, item.Id, uom.Id, qty: 1, unitCost: 100);
        var asOfDate = DateTimeOffset.UtcNow;
        invoice.DueDate = asOfDate.AddDays(-daysPastDue);
        db.ApInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetApAgingReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetApAgingReportQuery(null, null, asOfDate), TestContext.Current.CancellationToken);

        result.Value!.Groups.Single().Invoices.Single().Bucket.Should().Be(expectedBucket);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Filter_By_Branch_And_Supplier()
    {
        var (db, branch, supplier, item, uom) = await Seed();
        var otherBranch = LoanTestFixtures.Branch(id: "br-2");
        db.Branches.Add(otherBranch);
        var otherSupplier = PurchasingTestFixtures.Supplier(code: "SUP2");
        db.Suppliers.Add(otherSupplier);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ApInvoices.Add(PurchasingTestFixtures.ApInvoice(branch.Id, supplier.Id, item.Id, uom.Id));
        db.ApInvoices.Add(PurchasingTestFixtures.ApInvoice(otherBranch.Id, supplier.Id, item.Id, uom.Id));
        db.ApInvoices.Add(PurchasingTestFixtures.ApInvoice(branch.Id, otherSupplier.Id, item.Id, uom.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetApAgingReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetApAgingReportQuery(branch.Id, supplier.Id, null), TestContext.Current.CancellationToken);

        result.Value!.Groups.Should().ContainSingle();
        result.Value!.Groups.Single().Invoices.Should().ContainSingle();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("AP_INVOICES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetApAgingReportQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetApAgingReportQuery(null, null, null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
