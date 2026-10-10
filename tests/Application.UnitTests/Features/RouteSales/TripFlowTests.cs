namespace ZARI.Application.UnitTests.Features.RouteSales;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Features.Inventory.StockLedgers.Issue;
using ZARI.Application.Features.Inventory.StockLedgers.Receive;
using ZARI.Application.Features.RouteSales.Trips.Close;
using ZARI.Application.Features.RouteSales.Trips.Get;
using ZARI.Application.Features.RouteSales.Trips.Handheld;
using ZARI.Application.Features.RouteSales.Trips.LoadWorkflow;
using ZARI.Application.Features.RouteSales.Trips.RouteSales;
using ZARI.Application.Features.RouteSales.Trips.Shared;
using ZARI.Application.Features.Sales.CustomerPayments.Create;
using ZARI.Application.Features.Sales.PosSale;
using ZARI.Application.Features.Sales.SalesInvoices.Create;
using ZARI.Application.Features.Sales.SalesReturns.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class TripPlanningTests
{
    [Fact]
    public async Task Plan_Should_Load_The_Undelivered_Order_Quantity_Plus_Extra_Stock()
    {
        await using var bed = await TripTestBed.CreateAsync();

        var result = await bed.CreateTrip().HandleAsync(bed.PlanCommand(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue(result.Error?.Message);
        result.Value!.Status.Should().Be(TripStatus.Draft);
        result.Value.DriverEmployeeId.Should().Be(bed.EmployeeId, "the driver's login is linked to an employee record");
        var a = result.Value.Lines.Single(l => l.ItemId == bed.ItemA.Id);
        a.QtyOrdered.Should().Be(5);
        a.QtyExtra.Should().Be(3);
        result.Value.Lines.Single(l => l.ItemId == bed.ItemB.Id).QtyExtra.Should().Be(10);
    }

    [Fact]
    public async Task Plan_Should_Reject_An_Unapproved_Order_And_An_Order_Already_On_An_Open_Trip()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        (await bed.CreateTrip().HandleAsync(bed.PlanCommand(), ct)).IsSuccess.Should().BeTrue();

        var again = await bed.CreateTrip().HandleAsync(bed.PlanCommand(), ct);
        again.Error!.Code.Should().Be("Trip.SalesOrderOnAnotherTrip");

        bed.Order.Status = "DRAFT";
        await bed.Db.SaveChangesAsync(ct);
        var draft = await bed.CreateTrip().HandleAsync(bed.PlanCommand() with { SalesOrderIds = [bed.Order.Id] }, ct);
        draft.Error!.Code.Should().BeOneOf("Trip.SalesOrderNotApproved", "Trip.SalesOrderOnAnotherTrip");
    }

    [Fact]
    public async Task Available_Sales_Orders_Leave_Out_Ones_On_Another_Open_Trip()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var handler = new GetAvailableSalesOrdersQueryHandler(bed.Db, bed.Permissions);

        var before = await handler.HandleAsync(new GetAvailableSalesOrdersQuery("br-1", null), ct);
        var trip = (await bed.CreateTrip().HandleAsync(bed.PlanCommand(), ct)).Value!;
        var after = await handler.HandleAsync(new GetAvailableSalesOrdersQuery("br-1", null), ct);
        var editing = await handler.HandleAsync(new GetAvailableSalesOrdersQuery("br-1", trip.Id), ct);

        before.Value!.Single().Lines.Single().QtyRemaining.Should().Be(5);
        after.Value.Should().BeEmpty("the order is on an open trip");
        editing.Value.Should().ContainSingle(o => o.Id == bed.Order.Id, "the trip being edited still sees its own orders");
    }

    [Fact]
    public async Task Drivers_Are_The_Branchs_Users_With_Their_HR_Link()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        bed.Db.UserBranches.Add(new ZARI.Domain.Entities.UserBranch { UserId = bed.DriverId, BranchId = "br-1" });
        await bed.Db.SaveChangesAsync(ct);

        var drivers = await new GetTripDriversQueryHandler(bed.Db, bed.Permissions, IdentityTestFixtures.UserManager(bed.Db))
            .HandleAsync(new GetTripDriversQuery("br-1"), ct);

        var driver = drivers.Value!.Single();
        driver.Name.Should().Be("Pedro Driver");
        driver.EmployeeId.Should().Be(bed.EmployeeId);
        (await new GetTripDriversQueryHandler(bed.Db, bed.Permissions, IdentityTestFixtures.UserManager(bed.Db))
            .HandleAsync(new GetTripDriversQuery("br-other"), ct)).Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Plan_Should_Not_Load_From_A_Truck_Or_The_Damaged_Warehouse()
    {
        await using var bed = await TripTestBed.CreateAsync();

        var result = await bed.CreateTrip().HandleAsync(bed.PlanCommand() with { SourceWarehouseId = bed.DamagedWarehouseId }, TestContext.Current.CancellationToken);

        result.Error!.Code.Should().Be("Trip.InvalidSourceWarehouse");
    }

    [Fact]
    public async Task Approving_The_Load_Moves_Stock_From_The_Source_Onto_The_Truck()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var trip = (await bed.CreateTrip().HandleAsync(bed.PlanCommand(), ct)).Value!;
        await bed.SubmitTrip().HandleAsync(new SubmitTripCommand(trip.Id, "dispatcher"), ct);

        var result = await bed.ApproveLoad().HandleAsync(new ApproveTripLoadCommand(trip.Id, "manager", null), ct);

        result.Value!.Status.Should().Be(TripStatus.Loaded);
        result.Value.Lines.Single(l => l.ItemId == bed.ItemA.Id).QtyLoaded.Should().Be(8);
        await bed.Issue.Received(1).HandleAsync(Arg.Is<IssueStockLinesCommand>(c =>
            c.Lines.All(l => l.WarehouseId == bed.MainWarehouseId && l.ReferenceTable == TripStockMovePostingService.IssueReference)), Arg.Any<CancellationToken>());
        await bed.Receive.Received(2).HandleAsync(Arg.Is<ReceiveStockCommand>(c => c.WarehouseId == bed.TruckWarehouseId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_Truck_Can_Only_Be_Out_On_One_Trip_At_A_Time()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        await bed.TripOnTheRoadAsync();
        var second = (await bed.CreateTrip().HandleAsync(bed.PlanCommand() with { SalesOrderIds = [] }, ct)).Value!;
        await bed.SubmitTrip().HandleAsync(new SubmitTripCommand(second.Id, "dispatcher"), ct);

        var result = await bed.ApproveLoad().HandleAsync(new ApproveTripLoadCommand(second.Id, "manager", null), ct);

        result.Error!.Code.Should().Be("Trip.VehicleBusy");
    }

    [Fact]
    public async Task Accepting_The_Load_Records_Count_Discrepancies()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var trip = (await bed.CreateTrip().HandleAsync(bed.PlanCommand(), ct)).Value!;
        await bed.SubmitTrip().HandleAsync(new SubmitTripCommand(trip.Id, "dispatcher"), ct);
        await bed.ApproveLoad().HandleAsync(new ApproveTripLoadCommand(trip.Id, "manager", null), ct);

        var result = await bed.AcceptLoad().HandleAsync(new AcceptTripLoadCommand(trip.Id, [new(bed.ItemA.Id, 8), new(bed.ItemB.Id, 9)]), ct);

        result.Value!.Trip.Status.Should().Be(TripStatus.InTransit);
        result.Value.Discrepancies.Should().ContainSingle(d => d.ItemId == bed.ItemB.Id && d.Expected == 10 && d.Counted == 9);
    }
}

public sealed class RouteSaleTests
{
    [Fact]
    public async Task A_Stop_Records_Delivery_Invoice_With_The_Handheld_Number_And_Payment()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var tripId = await bed.TripOnTheRoadAsync();

        var result = await bed.RecordSale().HandleAsync(bed.Sale(tripId), ct);

        result.IsSuccess.Should().BeTrue(result.Error?.Message);
        result.Value!.Status.Should().Be("COMPLETED");
        result.Value.InvoiceTotal.Should().Be(672);
        result.Value.ChangeDue.Should().Be(28);

        var delivery = await bed.Db.DeliveryOrders.Include(d => d.Lines).SingleAsync(ct);
        delivery.WarehouseId.Should().Be(bed.TruckWarehouseId);
        delivery.Status.Should().Be("POSTED");
        delivery.Lines.Should().Contain(l => l.SalesOrderLineId == bed.Order.Lines[0].Id).And.Contain(l => l.SalesOrderLineId == null);

        var invoice = await bed.Db.SalesInvoices.SingleAsync(i => i.BirOrSeriesNumber != null, ct); // PAID once its payment posts
        invoice.BirOrSeriesNumber.Should().Be("T1-000501");
        invoice.PosTerminalId.Should().Be(bed.TerminalId);
        (await bed.Db.PosTerminals.AsNoTracking().SingleAsync(t => t.Id == bed.TerminalId, ct)).OrNextNumber.Should().Be(502);

        var payment = await bed.Db.CustomerPayments.Include(p => p.Tenders).SingleAsync(ct);
        payment.Tenders.Sum(t => t.Amount).Should().Be(672, "the 28 change comes out of the cash tender");

        var trip = await bed.Db.Trips.Include(t => t.Lines).AsNoTracking().SingleAsync(ct);
        trip.Lines.Single(l => l.ItemId == bed.ItemA.Id).QtyDelivered.Should().Be(5);
        trip.Lines.Single(l => l.ItemId == bed.ItemB.Id).QtyVanSold.Should().Be(2);
        trip.CashExpected.Should().Be(672);
    }

    [Fact]
    public async Task Sending_The_Same_Sale_Again_Books_Nothing_Twice()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var tripId = await bed.TripOnTheRoadAsync();
        var first = await bed.RecordSale().HandleAsync(bed.Sale(tripId), ct);

        var again = await bed.RecordSale().HandleAsync(bed.Sale(tripId), ct);

        again.Value!.Id.Should().Be(first.Value!.Id);
        (await bed.Db.DeliveryOrders.CountAsync(ct)).Should().Be(1);
        (await bed.Db.SalesInvoices.CountAsync(i => i.BirOrSeriesNumber != null, ct)).Should().Be(1);
        (await bed.Db.CustomerPayments.CountAsync(ct)).Should().Be(1);
        (await bed.Db.Trips.AsNoTracking().SingleAsync(ct)).CashExpected.Should().Be(672);
    }

    [Fact]
    public async Task A_Sale_That_Failed_Half_Way_Resumes_Without_A_Second_Delivery()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var tripId = await bed.TripOnTheRoadAsync();
        bed.SetGl(false); // the invoice's AR journal fails after the delivery already posted

        var failed = await bed.RecordSale().HandleAsync(bed.Sale(tripId), ct);
        failed.IsFailure.Should().BeTrue();
        var pending = await bed.Db.RouteSales.AsNoTracking().SingleAsync(ct);
        pending.Status.Should().Be("PENDING");
        pending.DeliveryOrderId.Should().NotBeNull();
        pending.LastError.Should().Contain("GL posting failed");

        bed.SetGl(true);
        var resumed = await bed.RecordSale().HandleAsync(bed.Sale(tripId), ct);

        resumed.Value!.Status.Should().Be("COMPLETED");
        (await bed.Db.DeliveryOrders.CountAsync(ct)).Should().Be(1);
        (await bed.Db.SalesInvoices.CountAsync(i => i.BirOrSeriesNumber == "T1-000501", ct)).Should().Be(1);
    }

    [Fact]
    public async Task Numbers_Must_Arrive_In_Sequence_And_A_Void_Consumes_One()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var tripId = await bed.TripOnTheRoadAsync();

        var skipped = await bed.RecordSale().HandleAsync(bed.Sale(tripId, "T1-000502"), ct);
        skipped.Error!.Code.Should().Be("PosTerminal.OutOfSequence");

        (await bed.VoidInvoice().HandleAsync(new VoidRouteInvoiceCommand(tripId, "T1-000501", "Customer cancelled"), ct)).Value!.Status.Should().Be("VOID");
        var next = await bed.RecordSale().HandleAsync(bed.Sale(tripId, "T1-000502"), ct);

        next.Value!.Status.Should().Be("COMPLETED");
    }

    [Fact]
    public async Task Selling_More_Than_Is_On_The_Truck_Is_Refused()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var tripId = await bed.TripOnTheRoadAsync();

        var result = await bed.RecordSale().HandleAsync(bed.Sale(tripId) with
        {
            SalesOrderId = null,
            Lines = [new RouteSaleLineInput(bed.ItemB.Id, 11, 56, 0)],
        }, ct);

        result.Error!.Code.Should().Be("RouteSale.NotOnTruck");
        (await bed.Db.RouteSales.CountAsync(ct)).Should().Be(0, "nothing is created for a sale that can't be recorded");
    }

    [Fact]
    public async Task A_Charge_Sale_Over_The_Credit_Limit_Is_Refused()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var tripId = await bed.TripOnTheRoadAsync();
        // Reload: earlier handlers cleared the change tracker, so bed.Customer is detached.
        var customer = await bed.Db.Customers.SingleAsync(c => c.Id == bed.Customer.Id, ct);
        customer.CreditLimit = 100;
        await bed.Db.SaveChangesAsync(ct);

        var result = await bed.RecordSale().HandleAsync(bed.Sale(tripId, cash: 0), ct);

        result.Error!.Code.Should().Be("RouteSale.CreditLimitExceeded");
    }

    [Fact]
    public async Task Only_The_Trips_Driver_Can_Record_Its_Sales()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var tripId = await bed.TripOnTheRoadAsync();
        bed.CurrentUser.UserId.Returns("someone-else");

        var result = await bed.RecordSale().HandleAsync(bed.Sale(tripId), ct);

        result.Error!.Code.Should().Be("RouteSale.NotTheDriver");
    }
}

public sealed class TripReturnCollectionAndCloseTests
{
    [Fact]
    public async Task Returns_Go_Into_The_Truck_And_Are_Idempotent()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var tripId = await bed.TripOnTheRoadAsync();
        var command = new RecordTripReturnCommand(tripId, Guid.NewGuid(), bed.Customer.Id, DateTimeOffset.UtcNow, "Expired",
            [new(bed.ItemB.Id, 1, 56, false), new(bed.ItemA.Id, 1, 112, true)]);

        (await bed.RecordReturn().HandleAsync(command, ct)).IsSuccess.Should().BeTrue();
        (await bed.RecordReturn().HandleAsync(command, ct)).IsSuccess.Should().BeTrue();

        await bed.SalesReturns.Received(1).HandleAsync(Arg.Is<CreateSalesReturnCommand>(c =>
            c.WarehouseId == bed.TruckWarehouseId && c.ForceQuickPost && c.Lines.Count == 2), Arg.Any<CancellationToken>());
        var lines = (await bed.Db.Trips.Include(t => t.Lines).AsNoTracking().SingleAsync(ct)).Lines;
        lines.Single(l => l.ItemId == bed.ItemB.Id).QtyReturnedGood.Should().Be(1);
        lines.Single(l => l.ItemId == bed.ItemA.Id).QtyReturnedDamaged.Should().Be(1);
    }

    [Fact]
    public async Task A_Collection_Posts_A_Payment_And_Adds_To_Expected_Cash_Once()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var tripId = await bed.TripOnTheRoadAsync();
        await bed.RecordSale().HandleAsync(bed.Sale(tripId, cash: 0), ct); // charge sale: 672 on account
        var invoiceId = (await bed.Db.SalesInvoices.SingleAsync(i => i.Status == "POSTED", ct)).Id;
        var command = new RecordTripCollectionCommand(tripId, Guid.NewGuid(), bed.Customer.Id, DateTimeOffset.UtcNow,
            [new TripCollectionAllocation(invoiceId, 300)], [new CustomerPaymentTenderInput(bed.Cash.Id, 300, null, null)]);

        (await bed.RecordCollection().HandleAsync(command, ct)).IsSuccess.Should().BeTrue();
        (await bed.RecordCollection().HandleAsync(command, ct)).IsSuccess.Should().BeTrue();

        (await bed.Db.CustomerPayments.CountAsync(ct)).Should().Be(1);
        (await bed.Db.Trips.AsNoTracking().SingleAsync(ct)).CashExpected.Should().Be(300);
    }

    [Fact]
    public async Task Ending_Requires_Every_Item_On_The_Truck_To_Be_Counted()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var tripId = await bed.TripOnTheRoadAsync();

        var result = await bed.EndTrip().HandleAsync(new EndTripCommand(tripId, 0, [new(bed.ItemA.Id, 8)]), TestContext.Current.CancellationToken);

        result.Error!.Code.Should().Be("Trip.CountMissing");
    }

    [Fact]
    public async Task Closing_Unloads_The_Truck_Sends_Damaged_Goods_Away_And_Charges_The_Driver()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var tripId = await bed.TripOnTheRoadAsync();
        await bed.RecordSale().HandleAsync(bed.Sale(tripId), ct); // A 5, B 2 sold; cash 672 expected
        await bed.RecordReturn().HandleAsync(new RecordTripReturnCommand(tripId, Guid.NewGuid(), bed.Customer.Id, DateTimeOffset.UtcNow, null,
            [new(bed.ItemB.Id, 1, 56, false), new(bed.ItemA.Id, 1, 112, true)]), ct);
        // Expected good on the truck: A = 8 - 5 = 3, B = 10 - 2 + 1 = 9. Driver counts B 8 (one short) and hands in 650 (22 short).
        (await bed.EndTrip().HandleAsync(new EndTripCommand(tripId, 650, [new(bed.ItemA.Id, 3), new(bed.ItemB.Id, 8)]), ct)).IsSuccess.Should().BeTrue();
        bed.Journals.Clear();

        var result = await bed.ApproveClose().HandleAsync(new ApproveTripCloseCommand(tripId, "manager", null), ct);

        result.IsSuccess.Should().BeTrue(result.Error?.Message);
        result.Value!.Status.Should().Be(TripStatus.Closed);
        result.Value.ShortageCharged.Should().Be(32, "1 unit of B at cost 10 plus 22 cash");
        result.Value.ShortageSettlement.Should().Be(TripShortageSettlement.Payroll, "the driver is linked to an HR employee");

        var moves = await bed.Db.TripStockMoves.Include(m => m.Lines).AsNoTracking().Where(m => m.TripId == tripId).ToListAsync(ct);
        moves.Single(m => m.MoveType == TripMoveType.Shortage).Lines.Single().Qty.Should().Be(1);
        var loadIn = moves.Single(m => m.MoveType == TripMoveType.LoadIn);
        loadIn.ToWarehouseId.Should().Be(bed.MainWarehouseId);
        loadIn.Lines.Sum(l => l.Qty).Should().Be(11);
        var damaged = moves.Single(m => m.MoveType == TripMoveType.Damaged);
        damaged.ToWarehouseId.Should().Be(bed.DamagedWarehouseId);
        damaged.Lines.Single().ItemId.Should().Be(bed.ItemA.Id);

        var journal = bed.Journals.Single(j => j.SourceReferenceTable == ApproveTripCloseCommandHandler.JournalTable);
        journal.Lines.Sum(l => l.DebitAmount).Should().Be(32);
        journal.Lines.Sum(l => l.CreditAmount).Should().Be(32);

        var deduction = await bed.Db.EmployeeDeductions.SingleAsync(ct);
        deduction.EmployeeId.Should().Be(bed.EmployeeId);
        deduction.Amount.Should().Be(32);
        deduction.EffectiveDate.Should().Be(deduction.EndDate!.Value, "it hits exactly one payroll");

        var lines = (await bed.Db.Trips.Include(t => t.Lines).AsNoTracking().SingleAsync(ct)).Lines;
        lines.Single(l => l.ItemId == bed.ItemB.Id).QtyVariance.Should().Be(-1);
        lines.Single(l => l.ItemId == bed.ItemA.Id).QtyDamagedOut.Should().Be(1);
    }

    [Fact]
    public async Task Closing_With_Damaged_Returns_Needs_A_Damaged_Warehouse()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var tripId = await bed.TripOnTheRoadAsync();
        await bed.RecordReturn().HandleAsync(new RecordTripReturnCommand(tripId, Guid.NewGuid(), bed.Customer.Id, DateTimeOffset.UtcNow, null,
            [new(bed.ItemA.Id, 1, 112, true)]), ct);
        await bed.EndTrip().HandleAsync(new EndTripCommand(tripId, 0, [new(bed.ItemA.Id, 8), new(bed.ItemB.Id, 10)]), ct);
        var damaged = await bed.Db.Warehouses.SingleAsync(w => w.Id == bed.DamagedWarehouseId, ct);
        damaged.Status = "inactive";
        await bed.Db.SaveChangesAsync(ct);

        var result = await bed.ApproveClose().HandleAsync(new ApproveTripCloseCommand(tripId, "manager", null), ct);

        result.Error!.Code.Should().Be("Trip.NoDamagedWarehouse");
        (await bed.Db.TripStockMoves.CountAsync(m => m.TripId == tripId && m.MoveType != TripMoveType.LoadOut, ct)).Should().Be(0,
            "nothing moves before the close can actually complete");
    }
}

public sealed class TruckTerminalGuardTests
{
    private static List<SalesInvoiceLineInput> Line(TripTestBed bed) =>
        [new SalesInvoiceLineInput(bed.ItemA.Id, 1, bed.ItemA.BaseUomId, 112, 0, null, null, "VATABLE", null, null, null)];

    [Fact]
    public async Task An_Ordinary_Invoice_Cannot_Use_A_Truck_Terminal()
    {
        await using var bed = await TripTestBed.CreateAsync();

        var result = await bed.CreateInvoice().HandleAsync(new CreateSalesInvoiceCommand("br-1", bed.Customer.Id, null, DateTimeOffset.UtcNow,
            null, null, null, null, "clerk", Line(bed), ForceQuickPost: true, PosTerminalId: bed.TerminalId), TestContext.Current.CancellationToken);

        result.Error!.Code.Should().Be("SalesInvoice.TruckTerminal", "only the truck's handheld issues the truck's BIR numbers");
    }

    [Fact]
    public async Task The_Pos_Counter_Cannot_Ring_Up_On_A_Truck_Terminal()
    {
        await using var bed = await TripTestBed.CreateAsync();

        var result = await bed.PosSale().HandleAsync(new CreatePosSaleCommand("br-1", bed.TerminalId, null, DateTimeOffset.UtcNow, null, null,
            Line(bed), [new CustomerPaymentTenderInput(bed.Cash.Id, 112, null, null)], "cashier"), TestContext.Current.CancellationToken);

        result.Error!.Code.Should().Be("PosSale.TruckTerminal");
    }
}
