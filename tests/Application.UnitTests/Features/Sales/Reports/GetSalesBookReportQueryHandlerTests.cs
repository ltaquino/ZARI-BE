namespace ZARI.Application.UnitTests.Features.Sales.Reports;

using ZARI.Application.Features.Sales.Reports.SalesBook;
using ZARI.Application.UnitTests.TestSupport;

public sealed class GetSalesBookReportQueryHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.Branch branch, Guid customerId, Guid itemId, Guid uomId)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var customer = LoanTestFixtures.Customer(branch.Id);
        db.Customers.Add(customer);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch, customer.Id, item.Id, uom.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branch, customerId, itemId, uomId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("SALES_INVOICES", ZARI.Domain.Common.FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetSalesBookReportQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetSalesBookReportQuery(null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ZARI.Domain.Common.ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Split_Vatable_Line_Into_Net_And_Vat()
    {
        var (db, branch, customerId, itemId, uomId) = await Seed();
        var invoice = SalesTestFixtures.SalesInvoice(branch.Id, customerId, itemId, uomId, status: "POSTED", qty: 1, unitPrice: 112);
        db.SalesInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetSalesBookReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetSalesBookReportQuery(null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value!.Rows.Single();
        row.Gross.Should().Be(112);
        row.VatableSales.Should().Be(100);
        row.VatAmount.Should().Be(12);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Bucket_Zero_Rated_And_Exempt_Lines_Untouched()
    {
        var (db, branch, customerId, itemId, uomId) = await Seed();
        var zeroRated = SalesTestFixtures.SalesInvoice(branch.Id, customerId, itemId, uomId, status: "POSTED", qty: 1, unitPrice: 50);
        zeroRated.Lines[0].VatType = "ZERO_RATED";
        var exempt = SalesTestFixtures.SalesInvoice(branch.Id, customerId, itemId, uomId, status: "POSTED", qty: 1, unitPrice: 30);
        exempt.Lines[0].VatType = "VAT_EXEMPT";
        db.SalesInvoices.AddRange(zeroRated, exempt);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetSalesBookReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetSalesBookReportQuery(null), TestContext.Current.CancellationToken);

        result.Value!.TotalZeroRated.Should().Be(50);
        result.Value!.TotalExempt.Should().Be(30);
        result.Value!.TotalVatAmount.Should().Be(0);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Exclude_Draft_Invoices()
    {
        var (db, branch, customerId, itemId, uomId) = await Seed();
        db.SalesInvoices.Add(SalesTestFixtures.SalesInvoice(branch.Id, customerId, itemId, uomId, status: "DRAFT"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetSalesBookReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetSalesBookReportQuery(null), TestContext.Current.CancellationToken);

        result.Value!.Rows.Should().BeEmpty();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Exclude_Cancelled_Invoices()
    {
        var (db, branch, customerId, itemId, uomId) = await Seed();
        db.SalesInvoices.Add(SalesTestFixtures.SalesInvoice(branch.Id, customerId, itemId, uomId, status: "CANCELLED"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetSalesBookReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetSalesBookReportQuery(null), TestContext.Current.CancellationToken);

        result.Value!.Rows.Should().BeEmpty();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Include_Pending_Approval_And_Partially_Paid_Invoices()
    {
        var (db, branch, customerId, itemId, uomId) = await Seed();
        db.SalesInvoices.Add(SalesTestFixtures.SalesInvoice(branch.Id, customerId, itemId, uomId, status: "PENDING_APPROVAL"));
        db.SalesInvoices.Add(SalesTestFixtures.SalesInvoice(branch.Id, customerId, itemId, uomId, status: "PARTIALLY_PAID"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetSalesBookReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetSalesBookReportQuery(null), TestContext.Current.CancellationToken);

        result.Value!.Rows.Should().HaveCount(2);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Filter_By_Branch()
    {
        var (db, branch, customerId, itemId, uomId) = await Seed();
        var otherBranch = LoanTestFixtures.Branch(id: "br-2");
        db.Branches.Add(otherBranch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.SalesInvoices.Add(SalesTestFixtures.SalesInvoice(branch.Id, customerId, itemId, uomId, status: "POSTED"));
        db.SalesInvoices.Add(SalesTestFixtures.SalesInvoice(otherBranch.Id, customerId, itemId, uomId, status: "POSTED"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetSalesBookReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetSalesBookReportQuery(branch.Id), TestContext.Current.CancellationToken);

        result.Value!.Rows.Should().ContainSingle();
        await db.DisposeAsync();
    }
}
