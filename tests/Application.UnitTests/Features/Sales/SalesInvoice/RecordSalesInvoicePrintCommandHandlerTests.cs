namespace ZARI.Application.UnitTests.Features.Sales.SalesInvoice;

using ZARI.Application.Features.Sales.SalesInvoices.RecordPrint;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class RecordSalesInvoicePrintCommandHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.SalesInvoice invoice)> Seed(string? birOrSeriesNumber = "BIR-OR-00001", int printCount = 0)
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
        var invoice = SalesTestFixtures.SalesInvoice(branch.Id, customer.Id, item.Id, uom.Id, status: birOrSeriesNumber is null ? "DRAFT" : "POSTED");
        invoice.BirOrSeriesNumber = birOrSeriesNumber;
        invoice.PrintCount = printCount;
        db.SalesInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, invoice);
    }

    [Fact]
    public async Task HandleAsync_Should_Record_First_Print_As_Not_A_Reprint()
    {
        var (db, invoice) = await Seed();
        var handler = new RecordSalesInvoicePrintCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new RecordSalesInvoicePrintCommand(invoice.Id, "cashier"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PrintCount.Should().Be(1);
        result.Value!.IsReprint.Should().BeFalse();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Record_Second_Print_As_A_Reprint()
    {
        var (db, invoice) = await Seed(printCount: 1);
        var handler = new RecordSalesInvoicePrintCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new RecordSalesInvoicePrintCommand(invoice.Id, "cashier"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PrintCount.Should().Be(2);
        result.Value!.IsReprint.Should().BeTrue();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new RecordSalesInvoicePrintCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new RecordSalesInvoicePrintCommand(Guid.NewGuid(), "cashier"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, invoice) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("SALES_INVOICES", FormAction.View, invoice.BranchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new RecordSalesInvoicePrintCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new RecordSalesInvoicePrintCommand(invoice.Id, "cashier"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Invoice_Has_No_BirOr_Number_Yet()
    {
        var (db, invoice) = await Seed(birOrSeriesNumber: null);
        var handler = new RecordSalesInvoicePrintCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new RecordSalesInvoicePrintCommand(invoice.Id, "cashier"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("SalesInvoice.NotPosted");
        await db.DisposeAsync();
    }
}
