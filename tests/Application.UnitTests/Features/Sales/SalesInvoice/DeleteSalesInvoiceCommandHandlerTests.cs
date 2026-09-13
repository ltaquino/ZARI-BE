namespace ZARI.Application.UnitTests.Features.Sales.SalesInvoice;

using ZARI.Application.Features.Sales.SalesInvoices.Delete;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class DeleteSalesInvoiceCommandHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.SalesInvoice invoice)> Seed(string status = "DRAFT")
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
        var invoice = SalesTestFixtures.SalesInvoice(branch.Id, customer.Id, item.Id, uom.Id, status: status);
        db.SalesInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, invoice);
    }

    [Fact]
    public async Task HandleAsync_Should_Delete_Draft_Invoice()
    {
        var (db, invoice) = await Seed();
        var handler = new DeleteSalesInvoiceCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteSalesInvoiceCommand(invoice.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.SalesInvoices.Should().BeEmpty();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteSalesInvoiceCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteSalesInvoiceCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, invoice) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("SALES_INVOICES", FormAction.Delete, invoice.BranchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteSalesInvoiceCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeleteSalesInvoiceCommand(invoice.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, invoice) = await Seed(status: "POSTED");
        var handler = new DeleteSalesInvoiceCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteSalesInvoiceCommand(invoice.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("SalesInvoice.NotDraft");
        await db.DisposeAsync();
    }
}
