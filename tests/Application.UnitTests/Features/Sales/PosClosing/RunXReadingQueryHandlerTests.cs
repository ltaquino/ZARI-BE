namespace ZARI.Application.UnitTests.Features.Sales.PosClosing;

using ZARI.Application.Features.Sales.PosClosing.RunXReading;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class RunXReadingQueryHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, Branch branch, Guid customerId, Guid itemId, Guid uomId)> Seed()
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
    public async Task HandleAsync_Should_Aggregate_Posted_Invoices_With_BirOr_Numbers()
    {
        var (db, branch, customerId, itemId, uomId) = await Seed();
        var invoice = SalesTestFixtures.SalesInvoice(branch.Id, customerId, itemId, uomId, status: "POSTED", qty: 1, unitPrice: 112);
        invoice.BirOrSeriesNumber = "000-000001";
        db.SalesInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new RunXReadingQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new RunXReadingQuery(branch.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.InvoiceCount.Should().Be(1);
        result.Value!.VatableSales.Should().Be(100);
        result.Value!.VatAmount.Should().Be(12);
        result.Value!.GrossSales.Should().Be(112);
        result.Value!.FirstOrNumber.Should().Be("000-000001");
        result.Value!.LastOrNumber.Should().Be("000-000001");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Exclude_Invoices_Without_A_BirOr_Number()
    {
        var (db, branch, customerId, itemId, uomId) = await Seed();
        db.SalesInvoices.Add(SalesTestFixtures.SalesInvoice(branch.Id, customerId, itemId, uomId, status: "DRAFT"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new RunXReadingQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new RunXReadingQuery(branch.Id), TestContext.Current.CancellationToken);

        result.Value!.InvoiceCount.Should().Be(0);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Exclude_Cancelled_Invoices_Even_With_A_BirOr_Number()
    {
        var (db, branch, customerId, itemId, uomId) = await Seed();
        var invoice = SalesTestFixtures.SalesInvoice(branch.Id, customerId, itemId, uomId, status: "CANCELLED");
        invoice.BirOrSeriesNumber = "000-000001";
        db.SalesInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new RunXReadingQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new RunXReadingQuery(branch.Id), TestContext.Current.CancellationToken);

        result.Value!.InvoiceCount.Should().Be(0);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Include_Partially_Paid_And_Paid_Invoices()
    {
        var (db, branch, customerId, itemId, uomId) = await Seed();
        var partiallyPaid = SalesTestFixtures.SalesInvoice(branch.Id, customerId, itemId, uomId, status: "PARTIALLY_PAID", qty: 1, unitPrice: 100);
        partiallyPaid.BirOrSeriesNumber = "000-000001";
        var paid = SalesTestFixtures.SalesInvoice(branch.Id, customerId, itemId, uomId, status: "PAID", qty: 1, unitPrice: 100);
        paid.BirOrSeriesNumber = "000-000002";
        db.SalesInvoices.AddRange(partiallyPaid, paid);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new RunXReadingQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new RunXReadingQuery(branch.Id), TestContext.Current.CancellationToken);

        result.Value!.InvoiceCount.Should().Be(2);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Only_Count_Invoices_Past_The_Last_ZReadings_Floor()
    {
        var (db, branch, customerId, itemId, uomId) = await Seed();
        db.ZReadings.Add(new ZReading
        {
            BranchId = branch.Id, ZCounterValue = 1, LastOrNumber = "000-000001", FirstOrNumber = "000-000001",
            PeriodStart = DateTimeOffset.UtcNow.AddDays(-1), PeriodEnd = DateTimeOffset.UtcNow.AddHours(-1), InvoiceCount = 1
        });
        var alreadyClosed = SalesTestFixtures.SalesInvoice(branch.Id, customerId, itemId, uomId, status: "POSTED");
        alreadyClosed.BirOrSeriesNumber = "000-000001";
        var newSale = SalesTestFixtures.SalesInvoice(branch.Id, customerId, itemId, uomId, status: "POSTED");
        newSale.BirOrSeriesNumber = "000-000002";
        db.SalesInvoices.AddRange(alreadyClosed, newSale);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new RunXReadingQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new RunXReadingQuery(branch.Id), TestContext.Current.CancellationToken);

        result.Value!.InvoiceCount.Should().Be(1);
        result.Value!.FirstOrNumber.Should().Be("000-000002");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new RunXReadingQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new RunXReadingQuery("br-missing"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branch, _, _, _) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("POS_CLOSING", FormAction.View, branch.Id, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new RunXReadingQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new RunXReadingQuery(branch.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}
