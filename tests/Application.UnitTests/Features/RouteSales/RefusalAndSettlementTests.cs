namespace ZARI.Application.UnitTests.Features.RouteSales;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Features.RouteSales.Reports.DriverShortages;
using ZARI.Application.Features.RouteSales.Trips.Close;
using ZARI.Application.Features.RouteSales.Trips.Handheld;
using ZARI.Application.Features.RouteSales.Trips.RouteSales;
using ZARI.Application.Features.RouteSales.Trips.Shared;
using ZARI.Application.UnitTests.TestSupport;

public sealed class RefusalTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_Partial_Delivery_Without_A_Reason_Is_Refused_And_Nothing_Is_Created()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var tripId = await bed.TripOnTheRoadAsync();

        var result = await bed.RecordSale().HandleAsync(bed.Sale(tripId) with
        {
            Lines = [new RouteSaleLineInput(bed.ItemA.Id, 3, 112, 0, bed.Order.Lines[0].Id)],
        }, Ct);

        result.Error!.Code.Should().Be("RouteSale.ShortfallReasonRequired");
        result.Error.Message.Should().Contain("2 of");
        (await bed.Db.RouteSales.CountAsync(Ct)).Should().Be(0);
    }

    [Fact]
    public async Task A_Partial_Delivery_Records_What_Was_Not_Received_And_Why()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var tripId = await bed.TripOnTheRoadAsync();

        var result = await bed.RecordSale().HandleAsync(bed.Sale(tripId) with
        {
            Lines = [new RouteSaleLineInput(bed.ItemA.Id, 3, 112, 0, bed.Order.Lines[0].Id)],
            Refusals = [new TripRefusalLineInput(bed.Order.Lines[0].Id, 2, "DAMAGED", "Two sacks wet")],
        }, Ct);

        result.IsSuccess.Should().BeTrue(result.Error?.Message);
        var refusal = await bed.Db.TripRefusals.Include(r => r.Lines).SingleAsync(Ct);
        refusal.RouteSaleId.Should().Be(result.Value!.Id);
        refusal.Lines.Single().Should().Match<ZARI.Domain.Entities.TripRefusalLine>(l => l.Qty == 2 && l.ReasonCode == "DAMAGED" && l.Notes == "Two sacks wet");
        var line = (await bed.Db.Trips.Include(t => t.Lines).AsNoTracking().SingleAsync(Ct)).Lines.Single(l => l.ItemId == bed.ItemA.Id);
        line.QtyDelivered.Should().Be(3);
        line.QtyRefused.Should().Be(2);
        TripShared.ExpectedGoodOnTruck(line).Should().Be(5, "refused goods never left the truck (8 loaded − 3 delivered)");
    }

    [Fact]
    public async Task A_Whole_Stop_Refused_Uses_No_Invoice_Number_And_Is_Idempotent()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var tripId = await bed.TripOnTheRoadAsync();
        var command = new RecordTripRefusalCommand(tripId, Guid.NewGuid(), bed.Customer.Id, bed.Order.Id, DateTimeOffset.UtcNow,
            [new TripRefusalLineInput(bed.Order.Lines[0].Id, 5, "STORE_CLOSED")]);
        var handler = new RecordTripRefusalCommandHandler(bed.Db, bed.CurrentUser, bed.Permissions);

        var first = await handler.HandleAsync(command, Ct);
        var again = await handler.HandleAsync(command, Ct);

        first.IsSuccess.Should().BeTrue(first.Error?.Message);
        again.Value.Should().Be(first.Value);
        (await bed.Db.TripRefusals.CountAsync(Ct)).Should().Be(1);
        (await bed.Db.Trips.Include(t => t.Lines).AsNoTracking().SingleAsync(Ct)).Lines.Single(l => l.ItemId == bed.ItemA.Id).QtyRefused.Should().Be(5);
        (await bed.Db.RouteSales.CountAsync(Ct)).Should().Be(0, "no invoice was issued");

        // Nothing of the order is open on this trip any more.
        var sale = await bed.RecordSale().HandleAsync(bed.Sale(tripId), Ct);
        sale.Error!.Code.Should().Be("RouteSale.ExceedsOrderedQty");
        var more = await handler.HandleAsync(command with { ClientRef = Guid.NewGuid() }, Ct);
        more.Error!.Code.Should().Be("TripRefusal.ExceedsOpen");
    }

    [Fact]
    public void Other_Needs_Notes_And_The_Reason_Must_Be_On_The_List()
    {
        var validator = new RecordTripRefusalValidator();
        RecordTripRefusalCommand Command(string code, string? notes) => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateTimeOffset.UtcNow, [new TripRefusalLineInput(Guid.NewGuid(), 1, code, notes)]);

        validator.Validate(Command(TripRefusalReason.Other, null)).IsValid.Should().BeFalse();
        validator.Validate(Command("FELT_LIKE_IT", null)).IsValid.Should().BeFalse();
        validator.Validate(Command(TripRefusalReason.Other, "Owner away")).IsValid.Should().BeTrue();
        validator.Validate(Command("CUSTOMER_REFUSED", null)).IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task The_Handheld_Download_Carries_The_Invoice_Header_And_Each_Stops_Open_Invoices()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var tripId = await bed.TripOnTheRoadAsync();
        (await bed.RecordSale().HandleAsync(bed.Sale(tripId, cash: 0), Ct)).IsSuccess.Should().BeTrue(); // 672 on account

        var bundle = (await new GetMyTripsQueryHandler(bed.Db, bed.CurrentUser).HandleAsync(new GetMyTripsQuery(), Ct)).Value!.Single();

        bundle.InvoiceHeader.Should().NotBeNull();
        bundle.InvoiceHeader!.BranchName.Should().NotBeNullOrEmpty();
        var stop = bundle.Stops.Single(s => s.CustomerId == bed.Customer.Id);
        stop.OpenBalance.Should().Be(672);
        stop.OpenInvoices.Should().ContainSingle(i => i.Balance == 672 && i.BirOrSeriesNumber == "T1-000501");
    }

    [Fact]
    public async Task The_Handheld_Download_Offers_The_Reasons_And_Excludes_Refused_Quantity()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var tripId = await bed.TripOnTheRoadAsync();
        await new RecordTripRefusalCommandHandler(bed.Db, bed.CurrentUser, bed.Permissions).HandleAsync(new RecordTripRefusalCommand(tripId, Guid.NewGuid(),
            bed.Customer.Id, bed.Order.Id, DateTimeOffset.UtcNow, [new TripRefusalLineInput(bed.Order.Lines[0].Id, 2, "OVERSTOCKED")]), Ct);

        var bundle = (await new GetMyTripsQueryHandler(bed.Db, bed.CurrentUser).HandleAsync(new GetMyTripsQuery(), Ct)).Value!.Single();

        bundle.RefusalReasons.Should().Contain(r => r.Code == "CUSTOMER_REFUSED");
        bundle.SalesOrders.Single().Lines.Single().QtyRemaining.Should().Be(3);
    }
}

public sealed class ShortageSettlementTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>The close scenario up to the driver ending: B one short (cost 10) and 22 cash short = 32.</summary>
    private static async Task<Guid> EndedTripAsync(TripTestBed bed, bool driverLinkedToHr = true)
    {
        if (!driverLinkedToHr)
        {
            var employee = await bed.Db.Employees.SingleAsync(e => e.Id == bed.EmployeeId, Ct);
            employee.ApplicationUserId = null;
            await bed.Db.SaveChangesAsync(Ct);
        }
        var tripId = await bed.TripOnTheRoadAsync();
        (await bed.RecordSale().HandleAsync(bed.Sale(tripId), Ct)).IsSuccess.Should().BeTrue();
        (await bed.EndTrip().HandleAsync(new EndTripCommand(tripId, 650, [new(bed.ItemA.Id, 3), new(bed.ItemB.Id, 7)]), Ct)).IsSuccess.Should().BeTrue();
        return tripId;
    }

    private static RecordTripShortagePaymentCommandHandler Payments(TripTestBed bed) => new(bed.Db, bed.Gl, bed.Permissions);

    [Fact]
    public async Task Payroll_Needs_The_Driver_Linked_To_HR()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var tripId = await EndedTripAsync(bed, driverLinkedToHr: false);

        var result = await bed.ApproveClose().HandleAsync(new ApproveTripCloseCommand(tripId, "manager", null, TripShortageSettlement.Payroll), Ct);

        result.Error!.Code.Should().Be("Trip.DriverNotLinkedToHr");
        (await bed.Db.TripStockMoves.CountAsync(m => m.MoveType != TripMoveType.LoadOut, Ct)).Should().Be(0);
    }

    [Fact]
    public async Task A_Driver_Not_Linked_To_HR_Pays_The_Shortage_In_And_The_Balance_Clears()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var tripId = await EndedTripAsync(bed, driverLinkedToHr: false);

        var closed = await bed.ApproveClose().HandleAsync(new ApproveTripCloseCommand(tripId, "manager", null), Ct);

        closed.IsSuccess.Should().BeTrue(closed.Error?.Message);
        closed.Value!.ShortageSettlement.Should().Be(TripShortageSettlement.DriverPays, "the default when the driver isn't linked to HR");
        closed.Value.ShortageCharged.Should().Be(32);
        closed.Value.ShortageBalance.Should().Be(32);
        (await bed.Db.EmployeeDeductions.CountAsync(Ct)).Should().Be(0, "nothing goes through payroll");

        var report = new GetDriverShortagesReportQueryHandler(bed.Db, bed.Permissions, IdentityTestFixtures.UserManager(bed.Db));
        (await report.HandleAsync(new GetDriverShortagesReportQuery("br-1", OutstandingOnly: true), Ct)).Value!.Rows.Single().Balance.Should().Be(32);

        bed.Journals.Clear();
        var first = await Payments(bed).HandleAsync(new RecordTripShortagePaymentCommand(tripId, 20, bed.Cash.Id, DateTimeOffset.UtcNow, "OR-1", null, "cashier"), Ct);
        first.IsSuccess.Should().BeTrue(first.Error?.Message);
        var journal = bed.Journals.Single();
        journal.SourceReferenceTable.Should().Be(RecordTripShortagePaymentCommandHandler.JournalTable);
        journal.Lines.Should().Contain(l => l.AccountId == bed.Cash.GlAccountId && l.DebitAmount == 20);
        journal.Lines.Sum(l => l.CreditAmount).Should().Be(20);

        (await Payments(bed).HandleAsync(new RecordTripShortagePaymentCommand(tripId, 13, bed.Cash.Id, DateTimeOffset.UtcNow, null, null, "cashier"), Ct))
            .Error!.Code.Should().Be("Trip.OverpaidShortage");
        (await Payments(bed).HandleAsync(new RecordTripShortagePaymentCommand(tripId, 12, bed.Cash.Id, DateTimeOffset.UtcNow, null, null, "cashier"), Ct))
            .IsSuccess.Should().BeTrue();

        var trip = await bed.Db.Trips.AsNoTracking().SingleAsync(Ct);
        trip.ShortagePaid.Should().Be(32);
        TripShared.ShortageBalance(trip).Should().Be(0);
        (await report.HandleAsync(new GetDriverShortagesReportQuery("br-1", OutstandingOnly: true), Ct)).Value!.Rows.Should().BeEmpty("nothing is owed any more");
        (await report.HandleAsync(new GetDriverShortagesReportQuery("br-1", OutstandingOnly: false), Ct)).Value!.Rows.Single().Paid.Should().Be(32);
    }

    [Fact]
    public async Task A_Payroll_Shortage_Takes_No_Driver_Payment()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var tripId = await EndedTripAsync(bed);
        (await bed.ApproveClose().HandleAsync(new ApproveTripCloseCommand(tripId, "manager", null), Ct)).Value!.ShortageSettlement.Should().Be(TripShortageSettlement.Payroll);

        var result = await Payments(bed).HandleAsync(new RecordTripShortagePaymentCommand(tripId, 10, bed.Cash.Id, DateTimeOffset.UtcNow, null, null, "cashier"), Ct);

        result.Error!.Code.Should().Be("Trip.NoDriverShortage");
    }

    [Fact]
    public async Task A_Written_Off_Shortage_Charges_Nobody()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var tripId = await EndedTripAsync(bed);
        bed.Journals.Clear();

        var result = await bed.ApproveClose().HandleAsync(new ApproveTripCloseCommand(tripId, "manager", null, "write_off"), Ct);

        result.IsSuccess.Should().BeTrue(result.Error?.Message);
        result.Value!.ShortageSettlement.Should().Be(TripShortageSettlement.WriteOff);
        result.Value.ShortageCharged.Should().Be(0);
        (await bed.Db.EmployeeDeductions.CountAsync(Ct)).Should().Be(0);
        var journal = bed.Journals.Single(j => j.SourceReferenceTable == ApproveTripCloseCommandHandler.JournalTable);
        journal.Lines.Sum(l => l.DebitAmount).Should().Be(32, "the shortage is still booked, as an expense");
    }

    [Fact]
    public async Task An_Unknown_Settlement_Is_Rejected()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var tripId = await EndedTripAsync(bed);

        var result = await bed.ApproveClose().HandleAsync(new ApproveTripCloseCommand(tripId, "manager", null, "SPLIT"), Ct);

        result.Error!.Code.Should().Be("Trip.InvalidShortageSettlement");
    }

    private static async Task TurnOffPayrollDeductionAsync(TripTestBed bed)
    {
        var company = SystemModuleTestFixtures.Company();
        company.RouteShortagePayrollDeductionEnabled = false;
        bed.Db.Companies.Add(company);
        await bed.Db.SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task With_Payroll_Deduction_Off_A_Shortage_Is_Only_Recorded_And_Reported()
    {
        await using var bed = await TripTestBed.CreateAsync();
        await TurnOffPayrollDeductionAsync(bed);
        var tripId = await EndedTripAsync(bed); // the driver IS linked to HR — still no payroll

        var result = await bed.ApproveClose().HandleAsync(new ApproveTripCloseCommand(tripId, "manager", null), Ct);

        result.IsSuccess.Should().BeTrue(result.Error?.Message);
        result.Value!.ShortageSettlement.Should().Be(TripShortageSettlement.WriteOff, "report-only is the default when payroll deduction is off");
        result.Value.ShortageAmount.Should().Be(32);
        result.Value.ShortageCharged.Should().Be(0);
        result.Value.ShortageBalance.Should().Be(0);
        (await bed.Db.EmployeeDeductions.CountAsync(Ct)).Should().Be(0);

        var report = await new GetDriverShortagesReportQueryHandler(bed.Db, bed.Permissions, IdentityTestFixtures.UserManager(bed.Db))
            .HandleAsync(new GetDriverShortagesReportQuery("br-1"), Ct);
        var row = report.Value!.Rows.Single();
        row.Shortage.Should().Be(32);
        row.WrittenOff.Should().Be(32);
        row.Charged.Should().Be(0);
        report.Value.Drivers.Single().Should().Match<DriverShortageSummary>(d => d.DriverName == "Pedro Driver" && d.Trips == 1 && d.Shortage == 32);
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);
        (await new GetDriverShortagesReportQueryHandler(bed.Db, bed.Permissions, IdentityTestFixtures.UserManager(bed.Db))
            .HandleAsync(new GetDriverShortagesReportQuery("br-1", From: today.AddDays(1)), Ct)).Value!.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task With_Payroll_Deduction_Off_Payroll_Is_Refused_But_Driver_Pays_Is_Allowed()
    {
        await using var bed = await TripTestBed.CreateAsync();
        await TurnOffPayrollDeductionAsync(bed);
        var tripId = await EndedTripAsync(bed);

        (await bed.ApproveClose().HandleAsync(new ApproveTripCloseCommand(tripId, "manager", null, TripShortageSettlement.Payroll), Ct))
            .Error!.Code.Should().Be("Trip.PayrollDeductionDisabled");
        var driverPays = await bed.ApproveClose().HandleAsync(new ApproveTripCloseCommand(tripId, "manager", null, TripShortageSettlement.DriverPays), Ct);

        driverPays.Value!.ShortageSettlement.Should().Be(TripShortageSettlement.DriverPays);
        driverPays.Value.ShortageBalance.Should().Be(32);
    }
}
