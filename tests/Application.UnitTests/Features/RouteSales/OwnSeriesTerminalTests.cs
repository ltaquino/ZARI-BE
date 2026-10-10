namespace ZARI.Application.UnitTests.Features.RouteSales;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Customers.Create;
using ZARI.Application.Features.Inventory.Warehouses.Update;
using ZARI.Application.Features.Sales.PosClosing.RunZReading;
using ZARI.Application.Features.Sales.PosTerminals.Shared;
using ZARI.Application.Features.Sales.PosTerminals.Update;
using ZARI.Application.Features.Sales.SalesInvoices.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Route Sales build step 1: a truck handheld (PosTerminal.HasOwnSeries) issues invoices from its own
/// BIR series and is closed on its own Z-Counter, while store tills keep the shared branch series.
/// </summary>
public sealed class OwnSeriesTerminalTests
{
    private static PosTerminal TruckTerminal(string code = "TRK1", int next = 501) => new()
    {
        BranchId = "br-1", Code = code, Name = "Truck handheld", Status = "active",
        HasOwnSeries = true, OrPrefix = "T1-", OrNextNumber = next, OrPaddingLength = 6,
    };

    private sealed record Seeded(ZARI.Infrastructure.Persistence.AppDbContext Db, Guid CustomerId, Guid ItemId, Guid UomId);

    private static async Task<Seeded> Seed()
    {
        var db = TestDbContextFactory.Create();
        db.Branches.Add(LoanTestFixtures.Branch());
        var customer = LoanTestFixtures.Customer();
        db.Customers.Add(customer);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        db.GlAccounts.AddRange(
            LoanTestFixtures.GlAccount("4000", "Sales Revenue", "Revenue", "Credit"),
            LoanTestFixtures.GlAccount("1200", "Accounts Receivable"),
            LoanTestFixtures.GlAccount("2200", "VAT Payable", "Liability", "Credit"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return new Seeded(db, customer.Id, item.Id, uom.Id);
    }

    private static SalesInvoice Invoice(Seeded s, Guid? terminalId, string? orNo = null, decimal price = 112)
    {
        var invoice = SalesTestFixtures.SalesInvoice("br-1", s.CustomerId, s.ItemId, s.UomId, unitPrice: price);
        invoice.PosTerminalId = terminalId;
        invoice.BirOrSeriesNumber = orNo;
        return invoice;
    }

    private static ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> GlOk() =>
        LoanTestFixtures.SuccessHandler<PostGlJournalCommand, Result<GlJournalResponse>>(Result.Success<GlJournalResponse>(default!));

    [Fact]
    public async Task Series_Should_Format_With_Prefix_And_Padding_And_Advance()
    {
        await using var db = TestDbContextFactory.Create();
        var terminal = TruckTerminal(next: 7);
        db.PosTerminals.Add(terminal);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var first = await PosTerminalSeries.NextAsync(db, terminal.Id, TestContext.Current.CancellationToken);
        var second = await PosTerminalSeries.NextAsync(db, terminal.Id, TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        first.Value.Should().Be("T1-000007");
        second.Value.Should().Be("T1-000008");
        (await db.PosTerminals.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).OrNextNumber.Should().Be(9);
    }

    [Fact]
    public async Task Posting_An_Invoice_From_A_Truck_Should_Use_The_Truck_Series_Not_The_Branch_Series()
    {
        var s = await Seed();
        var terminal = TruckTerminal();
        s.Db.PosTerminals.Add(terminal);
        s.Db.SalesInvoices.Add(Invoice(s, terminal.Id));
        await s.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var invoice = await s.Db.SalesInvoices.Include(i => i.Customer).Include(i => i.Lines).ThenInclude(l => l.Item)
            .SingleAsync(TestContext.Current.CancellationToken);
        var branchSeries = LoanTestFixtures.SuccessHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>>(
            Result.Success(new NextDocumentNumberResponse("HQ-OR-000001")));

        var result = await SalesInvoicePostingService.PostAsync(s.Db, branchSeries, GlOk(), invoice, TestContext.Current.CancellationToken);
        await s.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        invoice.BirOrSeriesNumber.Should().Be("T1-000501");
        await branchSeries.DidNotReceiveWithAnyArgs().HandleAsync(default!, default);
        (await s.Db.PosTerminals.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).OrNextNumber.Should().Be(502);
        await s.Db.DisposeAsync();
    }

    [Fact]
    public async Task Posting_An_Invoice_From_A_Store_Till_Should_Keep_The_Branch_Series()
    {
        var s = await Seed();
        var till = SalesTestFixtures.PosTerminal("br-1");
        s.Db.PosTerminals.Add(till);
        s.Db.SalesInvoices.Add(Invoice(s, till.Id));
        await s.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var invoice = await s.Db.SalesInvoices.Include(i => i.Customer).Include(i => i.Lines).ThenInclude(l => l.Item)
            .SingleAsync(TestContext.Current.CancellationToken);
        var branchSeries = LoanTestFixtures.SuccessHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>>(
            Result.Success(new NextDocumentNumberResponse("HQ-OR-000001")));

        await SalesInvoicePostingService.PostAsync(s.Db, branchSeries, GlOk(), invoice, TestContext.Current.CancellationToken);

        invoice.BirOrSeriesNumber.Should().Be("HQ-OR-000001");
        await s.Db.DisposeAsync();
    }

    private static RunZReadingCommandHandler ZHandler(ZARI.Infrastructure.Persistence.AppDbContext db) =>
        new(db, new CreateNotificationCommandHandler(db), LoanTestFixtures.AllowAllPermissionService());

    [Fact]
    public async Task Branch_And_Truck_Z_Readings_Should_Each_Close_Only_Their_Own_Series()
    {
        var s = await Seed();
        var terminal = TruckTerminal();
        s.Db.PosTerminals.Add(terminal);
        s.Db.SalesInvoices.AddRange(
            Invoice(s, null, "HQ-OR-000001", price: 100),
            Invoice(s, terminal.Id, "T1-000501", price: 200),
            Invoice(s, terminal.Id, "T1-000502", price: 300));
        await s.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var branchZ = await ZHandler(s.Db).HandleAsync(new RunZReadingCommand("br-1", "manager"), TestContext.Current.CancellationToken);
        var truckZ = await ZHandler(s.Db).HandleAsync(new RunZReadingCommand("br-1", "manager", terminal.Id), TestContext.Current.CancellationToken);

        branchZ.Value!.InvoiceCount.Should().Be(1);
        branchZ.Value.GrossSales.Should().Be(100);
        branchZ.Value.ZCounterValue.Should().Be(1);

        truckZ.Value!.InvoiceCount.Should().Be(2);
        truckZ.Value.GrossSales.Should().Be(500);
        truckZ.Value.FirstOrNumber.Should().Be("T1-000501");
        truckZ.Value.ZCounterValue.Should().Be(1, "the truck has its own Z-Counter");
        truckZ.Value.PosTerminalId.Should().Be(terminal.Id);

        (await s.Db.Branches.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).ZCounter.Should().Be(1);
        (await s.Db.PosTerminals.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).ZCounter.Should().Be(1);
        s.Db.ZReadings.Select(z => z.CounterScope).Should().BeEquivalentTo([ZReading.BranchScope, terminal.Id.ToString()]);
        await s.Db.DisposeAsync();
    }

    [Fact]
    public async Task A_Shared_Series_Till_Cannot_Be_Closed_On_Its_Own()
    {
        var s = await Seed();
        var till = SalesTestFixtures.PosTerminal("br-1");
        s.Db.PosTerminals.Add(till);
        await s.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await ZHandler(s.Db).HandleAsync(new RunZReadingCommand("br-1", "manager", till.Id), TestContext.Current.CancellationToken);

        result.Error!.Code.Should().Be("PosClosing.TerminalSharesBranchSeries");
        await s.Db.DisposeAsync();
    }

    [Fact]
    public async Task Terminal_Series_Should_Lock_Once_It_Has_Issued_A_Number()
    {
        var s = await Seed();
        var terminal = TruckTerminal();
        s.Db.PosTerminals.Add(terminal);
        s.Db.SalesInvoices.Add(Invoice(s, terminal.Id, "T1-000501"));
        await s.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await new UpdatePosTerminalCommandHandler(s.Db, LoanTestFixtures.AllowAllPermissionService()).HandleAsync(
            new UpdatePosTerminalCommand(terminal.Id, "TRK1", "Truck handheld", null, null, null, null, "active", true, "T9-", 1, 6),
            TestContext.Current.CancellationToken);

        result.Error!.Code.Should().Be("PosTerminal.SeriesLocked");
        await s.Db.DisposeAsync();
    }

    [Fact]
    public async Task A_Truck_Warehouse_Cannot_Be_Turned_Into_An_Ordinary_Warehouse()
    {
        await using var db = TestDbContextFactory.Create();
        db.Branches.Add(LoanTestFixtures.Branch());
        var warehouse = InventoryTestFixtures.Warehouse("br-1", code: "TRK-TRK1", warehouseType: "Vehicle");
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await new UpdateWarehouseCommandHandler(db, LoanTestFixtures.AllowAllPermissionService()).HandleAsync(
            new UpdateWarehouseCommand(warehouse.Id, "br-1", "TRK-TRK1", "Truck", "Main", "active"), TestContext.Current.CancellationToken);

        result.Error!.Code.Should().Be("Warehouse.VehicleTypeLocked");
    }

    [Fact]
    public void Customer_Credit_Limit_Cannot_Be_Negative()
    {
        var command = new CreateCustomerCommand("Store", "member", "a@b.c", "0917", "br-1", "active", "owner", "addr", null, null, null, null,
            CreditLimit: -1);

        new CreateCustomerValidator().Validate(command).Errors.Should().Contain(e => e.PropertyName == "CreditLimit");
    }
}
