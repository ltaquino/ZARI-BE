namespace ZARI.Application.UnitTests.Features.RouteSales;

using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Inventory.StockLedgers.Issue;
using ZARI.Application.Features.Inventory.StockLedgers.Receive;
using ZARI.Application.Features.RouteSales.Trips.Close;
using ZARI.Application.Features.RouteSales.Trips.Create;
using ZARI.Application.Features.RouteSales.Trips.Handheld;
using ZARI.Application.Features.RouteSales.Trips.LoadWorkflow;
using ZARI.Application.Features.RouteSales.Trips.RouteSales;
using ZARI.Application.Features.RouteSales.Trips.Shared;
using ZARI.Application.Features.RouteSales.Vehicles.Create;
using ZARI.Application.Features.Inventory.SerialNumbers.Issue;
using ZARI.Application.Features.Sales.CustomerPayments.Create;
using ZARI.Application.Features.Sales.PosSale;
using ZARI.Application.Features.Sales.SalesInvoices.Create;
using ZARI.Application.Features.Sales.SalesReturns.Create;
using ZARI.Application.Features.Sales.SalesReturns.GetAll;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;
using ZARI.Infrastructure.Persistence;

/// <summary>
/// A branch with a main and a Damaged warehouse, a truck (own series T1-, next 501), a driver user
/// linked to an employee, two items, a CASH method and an approved sales order (item A x5 @112).
/// The stock engines and the approval decide are fakes (both use ExecuteUpdate/real transactions,
/// which the InMemory provider can't run); everything else is the real handler.
/// </summary>
internal sealed class TripTestBed : IAsyncDisposable
{
    public AppDbContext Db { get; } = TestDbContextFactory.Create();
    public IPermissionService Permissions { get; } = LoanTestFixtures.AllowAllPermissionService();
    public ICurrentUser CurrentUser { get; } = Substitute.For<ICurrentUser>();
    public ICommandHandler<IssueStockLinesCommand, Result<IssueStockLinesResponse>> Issue { get; } = Substitute.For<ICommandHandler<IssueStockLinesCommand, Result<IssueStockLinesResponse>>>();
    public ICommandHandler<ReceiveStockCommand, Result<ReceiveStockResponse>> Receive { get; } = Substitute.For<ICommandHandler<ReceiveStockCommand, Result<ReceiveStockResponse>>>();
    public ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> Gl { get; private set; } = default!;
    public List<PostGlJournalCommand> Journals { get; } = [];

    public Guid DriverUserId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Guid TruckWarehouseId { get; private set; }
    public Guid TerminalId { get; private set; }
    public Guid MainWarehouseId { get; private set; }
    public Guid DamagedWarehouseId { get; private set; }
    public Item ItemA { get; private set; } = default!;
    public Item ItemB { get; private set; } = default!;
    public Customer Customer { get; private set; } = default!;
    public SalesOrder Order { get; private set; } = default!;
    public PaymentMethod Cash { get; private set; } = default!;

    public string DriverId => DriverUserId.ToString();

    public static async Task<TripTestBed> CreateAsync()
    {
        var bed = new TripTestBed();
        await bed.SeedAsync();
        return bed;
    }

    public void SetGl(bool succeed)
    {
        Gl = Substitute.For<ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>>>();
        Gl.HandleAsync(Arg.Do<PostGlJournalCommand>(c => Journals.Add(c)), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(succeed
                ? Result.Success<GlJournalResponse>(default!)
                : Result.Failure<GlJournalResponse>(Error.Failure("Gl.Down", "GL posting failed."))));
    }

    private async Task SeedAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        SetGl(true);
        // Costs are reported only for the trip's own stock moves: the existing Delivery posting writes
        // reported costs back with ExecuteUpdate, which InMemory can't run (the live API run covers it).
        Issue.HandleAsync(Arg.Any<IssueStockLinesCommand>(), Arg.Any<CancellationToken>()).Returns(ci =>
            Task.FromResult(Result.Success(new IssueStockLinesResponse(ci.Arg<IssueStockLinesCommand>().Lines
                .Where(l => l.ReferenceTable.StartsWith("TripStockMove", StringComparison.Ordinal))
                .ToDictionary(l => l.ReferenceId, _ => 10m)))));
        Receive.HandleAsync(Arg.Any<ReceiveStockCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success(new ReceiveStockResponse(10m))));

        Db.Branches.Add(LoanTestFixtures.Branch());
        var uom = InventoryTestFixtures.Uom();
        Db.Uoms.Add(uom);
        var cashAccount = LoanTestFixtures.GlAccount("1000", "Cash on Hand");
        Db.GlAccounts.AddRange(cashAccount,
            LoanTestFixtures.GlAccount("1200", "Accounts Receivable"),
            LoanTestFixtures.GlAccount("1220", "Due from Employees"),
            LoanTestFixtures.GlAccount("1400", "Inventory Asset"),
            LoanTestFixtures.GlAccount("2200", "VAT Payable", "Liability", "Credit"),
            LoanTestFixtures.GlAccount("4000", "Sales Revenue", "Revenue", "Credit"),
            LoanTestFixtures.GlAccount("4100", "Sales Returns", "Revenue", "Debit"),
            LoanTestFixtures.GlAccount("5000", "COGS", "Cogs"),
            LoanTestFixtures.GlAccount("5100", "Inventory Variance", "Cogs"),
            LoanTestFixtures.GlAccount("6900", "Other Operating Expense", "Expense"));
        var main = InventoryTestFixtures.Warehouse("br-1", "WH-MAIN", "Main", "Main");
        var damaged = InventoryTestFixtures.Warehouse("br-1", "WH-DMG", "Damaged", "Damaged");
        Db.Warehouses.AddRange(main, damaged);
        Customer = LoanTestFixtures.Customer(name: "Alpha Store");
        Db.Customers.Add(Customer);
        await Db.SaveChangesAsync(ct);

        Cash = LoanTestFixtures.PaymentMethod(cashAccount.Id);
        Db.PaymentMethods.Add(Cash);
        ItemA = InventoryTestFixtures.Item(uom.Id, code: "RICE-25", name: "Rice 25kg");
        ItemB = InventoryTestFixtures.Item(uom.Id, code: "SUGAR-1", name: "Sugar 1kg");
        Db.Items.AddRange(ItemA, ItemB);
        await Db.SaveChangesAsync(ct);
        MainWarehouseId = main.Id;
        DamagedWarehouseId = damaged.Id;

        var userManager = IdentityTestFixtures.UserManager(Db);
        var driver = new ApplicationUser { UserName = "driver@zari.coop", Email = "driver@zari.coop", FirstName = "Pedro", LastName = "Driver" };
        (await userManager.CreateAsync(driver, "Driver123")).Succeeded.Should().BeTrue();
        DriverUserId = Guid.Parse(driver.Id);
        CurrentUser.UserId.Returns(driver.Id);

        var employee = HrTestFixtures.Employee();
        employee.ApplicationUserId = driver.Id;
        Db.Employees.Add(employee);
        await Db.SaveChangesAsync(ct);
        EmployeeId = employee.Id;

        var vehicle = await new CreateVehicleCommandHandler(Db, Permissions)
            .HandleAsync(new CreateVehicleCommand("br-1", "TRK1", "ABC 1234", null, "active", "T1-", OrNextNumber: 501), ct);
        VehicleId = vehicle.Value!.Id;
        TruckWarehouseId = vehicle.Value.WarehouseId;
        TerminalId = vehicle.Value.PosTerminalId;

        Order = SalesTestFixtures.SalesOrder("br-1", Customer.Id, ItemA.Id, uom.Id, status: "POSTED", qty: 5, unitPrice: 112);
        Db.SalesOrders.Add(Order);
        await Db.SaveChangesAsync(ct);
    }

    // ---- handler factories (real handlers, fake stock/decide/GL)

    private GetNextDocumentNumberCommandHandler NextNumber => new(Db);
    private CreateNotificationCommandHandler Notify => new(Db);
    private ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> Decide =>
        LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success<ApprovalRequestResponse>(default!));

    public CreateTripCommandHandler CreateTrip() => new(Db, IdentityTestFixtures.UserManager(Db), NextNumber, Permissions);
    public SubmitTripCommandHandler SubmitTrip() => new(Db, new SubmitForApprovalCommandHandler(Db), Notify, Permissions);
    public ApproveTripLoadCommandHandler ApproveLoad() => new(Db, Decide, Issue, Receive, Notify, Permissions);
    public AcceptTripLoadCommandHandler AcceptLoad() => new(Db, CurrentUser, Notify);
    public RecordRouteSaleCommandHandler RecordSale() => new(Db, CurrentUser, Permissions, NextNumber, Issue, Gl,
        new CreateSalesInvoiceCommandHandler(Db, NextNumber, Gl, Notify, Permissions),
        new CreateCustomerPaymentCommandHandler(Db, NextNumber, Gl, Notify, Permissions));
    public VoidRouteInvoiceCommandHandler VoidInvoice() => new(Db, CurrentUser, Permissions);
    public CreateSalesInvoiceCommandHandler CreateInvoice() => new(Db, NextNumber, Gl, Notify, Permissions);
    public CreatePosSaleCommandHandler PosSale() => new(Db, Permissions, CreateInvoice(), Issue,
        Substitute.For<ICommandHandler<IssueSerialCommand, Result>>(), Gl, new CreateCustomerPaymentCommandHandler(Db, NextNumber, Gl, Notify, Permissions));
    /// <summary>CreateSalesReturn's quick-post flips its status with ExecuteUpdate (InMemory can't),
    /// so it's faked here and asserted on what it was asked to do.</summary>
    public ICommandHandler<CreateSalesReturnCommand, Result<SalesReturnResponse>> SalesReturns { get; } = FakeSalesReturns();

    private static ICommandHandler<CreateSalesReturnCommand, Result<SalesReturnResponse>> FakeSalesReturns()
    {
        var fake = Substitute.For<ICommandHandler<CreateSalesReturnCommand, Result<SalesReturnResponse>>>();
        fake.HandleAsync(Arg.Any<CreateSalesReturnCommand>(), Arg.Any<CancellationToken>()).Returns(ci =>
        {
            var c = ci.Arg<CreateSalesReturnCommand>();
            return Task.FromResult(Result.Success(new SalesReturnResponse(Guid.NewGuid(), "SRTN-1", c.BranchId, c.WarehouseId, c.CustomerId, "",
                null, c.ReturnDate, "POSTED", c.Remarks, [], null, null, null, null, DateTimeOffset.UtcNow, c.CreatedBy)));
        });
        return fake;
    }

    public RecordTripReturnCommandHandler RecordReturn() => new(Db, CurrentUser, Permissions, SalesReturns);
    public RecordTripCollectionCommandHandler RecordCollection() => new(Db, CurrentUser, Permissions,
        new CreateCustomerPaymentCommandHandler(Db, NextNumber, Gl, Notify, Permissions));
    public EndTripCommandHandler EndTrip() => new(Db, CurrentUser, Permissions, new SubmitForApprovalCommandHandler(Db), Notify);
    public ApproveTripCloseCommandHandler ApproveClose() => new(Db, Decide, Issue, Receive, Gl, Notify, Permissions);

    // ---- scenario helpers

    public CreateTripCommand PlanCommand() => new("br-1", VehicleId, null, MainWarehouseId, DriverId, null, "Jun helper",
        DateTimeOffset.UtcNow, null, [Order.Id], [new TripExtraLineInput(ItemA.Id, 3), new TripExtraLineInput(ItemB.Id, 10)], "dispatcher");

    /// <summary>Planned, approved and accepted: A loaded 8 (5 ordered + 3 extra), B loaded 10.</summary>
    public async Task<Guid> TripOnTheRoadAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var trip = await CreateTrip().HandleAsync(PlanCommand(), ct);
        trip.IsSuccess.Should().BeTrue(trip.Error?.Message);
        (await SubmitTrip().HandleAsync(new SubmitTripCommand(trip.Value!.Id, "dispatcher"), ct)).IsSuccess.Should().BeTrue();
        (await ApproveLoad().HandleAsync(new ApproveTripLoadCommand(trip.Value.Id, "manager", null), ct)).IsSuccess.Should().BeTrue();
        (await AcceptLoad().HandleAsync(new AcceptTripLoadCommand(trip.Value.Id, [new(ItemA.Id, 8), new(ItemB.Id, 10)]), ct)).IsSuccess.Should().BeTrue();
        return trip.Value.Id;
    }

    /// <summary>A x5 against the order (560) + B x2 van sale (112) = 672.</summary>
    public RecordRouteSaleCommand Sale(Guid tripId, string number = "T1-000501", decimal cash = 700) => new(
        tripId, number, Customer.Id, Order.Id, DateTimeOffset.UtcNow, null,
        [new RouteSaleLineInput(ItemA.Id, 5, 112, 0, Order.Lines[0].Id), new RouteSaleLineInput(ItemB.Id, 2, 56, 0)],
        cash > 0 ? [new CustomerPaymentTenderInput(Cash.Id, cash, null, null)] : []);

    public ValueTask DisposeAsync() => Db.DisposeAsync();
}
