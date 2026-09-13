namespace ZARI.Application.UnitTests.Features.Sales.PosClosing;

using ZARI.Application.Features.Sales.PosClosing.RunZReading;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class RunZReadingCommandHandlerTests
{
    private static RunZReadingCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

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
    public async Task HandleAsync_Should_Persist_A_ZReading_And_Increment_Branch_ZCounter()
    {
        var (db, branch, customerId, itemId, uomId) = await Seed();
        var invoice = SalesTestFixtures.SalesInvoice(branch.Id, customerId, itemId, uomId, status: "POSTED", qty: 1, unitPrice: 112);
        invoice.BirOrSeriesNumber = "000-000001";
        db.SalesInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new RunZReadingCommand(branch.Id, "manager"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ZCounterValue.Should().Be(1);
        result.Value!.InvoiceCount.Should().Be(1);
        result.Value!.GrossSales.Should().Be(112);
        db.ZReadings.Should().ContainSingle(z => z.BranchId == branch.Id);
        (await db.Branches.FindAsync([branch.Id], TestContext.Current.CancellationToken))!.ZCounter.Should().Be(1);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Allow_A_Zero_Invoice_Close()
    {
        var (db, branch, _, _, _) = await Seed();

        var result = await Handler(db).HandleAsync(new RunZReadingCommand(branch.Id, "manager"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.InvoiceCount.Should().Be(0);
        result.Value!.FirstOrNumber.Should().BeNull();
        result.Value!.LastOrNumber.Should().BeNull();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Increment_ZCounter_Across_Successive_Runs()
    {
        var (db, branch, _, _, _) = await Seed();
        await Handler(db).HandleAsync(new RunZReadingCommand(branch.Id, "manager"), TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new RunZReadingCommand(branch.Id, "manager"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ZCounterValue.Should().Be(2);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new RunZReadingCommand("br-missing", "manager"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branch, _, _, _) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("POS_CLOSING", FormAction.Create, branch.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new RunZReadingCommand(branch.Id, "manager"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}
