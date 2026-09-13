namespace ZARI.Application.UnitTests.Features.Purchasing.Reports.PurchaseBook;

using ZARI.Application.Features.Purchasing.Reports.PurchaseBook;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetPurchaseBookReportQueryHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.Branch branch, ZARI.Domain.Entities.Supplier supplier, ZARI.Domain.Entities.Item item, ZARI.Domain.Entities.Uom uom)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var supplier = PurchasingTestFixtures.Supplier();
        supplier.TaxId = "123-456-789-000";
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
    public async Task HandleAsync_Should_Split_Vatable_Line_Into_Net_And_Input_Tax()
    {
        var (db, branch, supplier, item, uom) = await Seed();
        var invoice = PurchasingTestFixtures.ApInvoice(branch.Id, supplier.Id, item.Id, uom.Id, qty: 1, unitCost: 112);
        db.ApInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetPurchaseBookReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetPurchaseBookReportQuery(null, null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value!.Rows.Single();
        row.Gross.Should().Be(112);
        row.VatableSales.Should().Be(100);
        row.InputTax.Should().Be(12);
        row.SupplierTaxId.Should().Be("123-456-789-000");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Bucket_Zero_Rated_And_Exempt_Lines_Untouched()
    {
        var (db, branch, supplier, item, uom) = await Seed();
        var zeroRated = PurchasingTestFixtures.ApInvoice(branch.Id, supplier.Id, item.Id, uom.Id, qty: 1, unitCost: 50);
        zeroRated.Lines[0].VatType = "ZERO_RATED";
        var exempt = PurchasingTestFixtures.ApInvoice(branch.Id, supplier.Id, item.Id, uom.Id, qty: 1, unitCost: 30);
        exempt.Lines[0].VatType = "VAT_EXEMPT";
        db.ApInvoices.AddRange(zeroRated, exempt);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetPurchaseBookReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetPurchaseBookReportQuery(null, null), TestContext.Current.CancellationToken);

        result.Value!.TotalZeroRated.Should().Be(50);
        result.Value!.TotalExempt.Should().Be(30);
        result.Value!.TotalInputTax.Should().Be(0);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Sum_Expense_Lines_For_Expense_Invoices()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var supplier = PurchasingTestFixtures.Supplier();
        db.Suppliers.Add(supplier);
        var glAccount = LoanTestFixtures.GlAccount(code: "6000", accountType: "Expense");
        db.GlAccounts.Add(glAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var invoice = PurchasingTestFixtures.ApInvoiceExpense(branch.Id, supplier.Id, glAccount.Id, status: "POSTED", amount: 112);
        db.ApInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetPurchaseBookReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetPurchaseBookReportQuery(null, null), TestContext.Current.CancellationToken);

        var row = result.Value!.Rows.Single();
        row.Gross.Should().Be(112);
        row.VatableSales.Should().Be(100);
        row.InputTax.Should().Be(12);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Ignore_Non_Posted_Invoices()
    {
        var (db, branch, supplier, item, uom) = await Seed();
        db.ApInvoices.Add(PurchasingTestFixtures.ApInvoice(branch.Id, supplier.Id, item.Id, uom.Id, status: "DRAFT"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetPurchaseBookReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetPurchaseBookReportQuery(null, null), TestContext.Current.CancellationToken);

        result.Value!.Rows.Should().BeEmpty();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Filter_By_Branch_And_Supplier()
    {
        var (db, branch, supplier, item, uom) = await Seed();
        var otherSupplier = PurchasingTestFixtures.Supplier(code: "SUP2");
        db.Suppliers.Add(otherSupplier);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ApInvoices.Add(PurchasingTestFixtures.ApInvoice(branch.Id, supplier.Id, item.Id, uom.Id));
        db.ApInvoices.Add(PurchasingTestFixtures.ApInvoice(branch.Id, otherSupplier.Id, item.Id, uom.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetPurchaseBookReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetPurchaseBookReportQuery(branch.Id, supplier.Id), TestContext.Current.CancellationToken);

        result.Value!.Rows.Should().ContainSingle();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("AP_INVOICES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetPurchaseBookReportQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetPurchaseBookReportQuery(null, null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
