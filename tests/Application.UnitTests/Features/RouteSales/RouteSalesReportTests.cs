namespace ZARI.Application.UnitTests.Features.RouteSales;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.RouteSales.Reports.RoutePerformance;
using ZARI.Application.Features.RouteSales.Reports.TripCashRemittance;
using ZARI.Application.Features.RouteSales.Reports.TripDeliveries;
using ZARI.Application.Features.RouteSales.Reports.TripSummary;
using ZARI.Application.Features.RouteSales.Reports.UndeliveredOrders;
using ZARI.Application.Features.RouteSales.Trips.Close;
using ZARI.Application.Features.RouteSales.Trips.Handheld;
using ZARI.Application.Features.RouteSales.Trips.RouteSales;
using ZARI.Application.Features.RouteSales.Trips.Shared;
using ZARI.Application.Features.Sales.CustomerPayments.Create;
using ZARI.Application.Features.Sales.Reports.SalesBook;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class RouteSalesReportTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// The close test's scenario: A 5 + B 2 sold for 672 cash, B 1 good + A 1 damaged returned, the driver
    /// counts B one short (cost 10) and hands in 650 (22 short); closed with the shortage charged.
    /// </summary>
    private static async Task<Guid> ClosedTripAsync(TripTestBed bed)
    {
        var tripId = await bed.TripOnTheRoadAsync();
        (await bed.RecordSale().HandleAsync(bed.Sale(tripId), Ct)).IsSuccess.Should().BeTrue();
        await bed.RecordReturn().HandleAsync(new RecordTripReturnCommand(tripId, Guid.NewGuid(), bed.Customer.Id, DateTimeOffset.UtcNow, null,
            [new(bed.ItemB.Id, 1, 56, false), new(bed.ItemA.Id, 1, 112, true)]), Ct);
        (await bed.EndTrip().HandleAsync(new EndTripCommand(tripId, 650, [new(bed.ItemA.Id, 3), new(bed.ItemB.Id, 8)]), Ct)).IsSuccess.Should().BeTrue();
        (await bed.ApproveClose().HandleAsync(new ApproveTripCloseCommand(tripId, "manager", null), Ct)).IsSuccess.Should().BeTrue();
        return tripId;
    }

    private static GetTripSummaryReportQueryHandler Summary(TripTestBed bed, IPermissionService? permissions = null) =>
        new(bed.Db, permissions ?? bed.Permissions, bed.CurrentUser, IdentityTestFixtures.UserManager(bed.Db));

    [Fact]
    public async Task Trip_Summary_Shows_Every_Quantity_And_The_Posted_Variance()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var tripId = await ClosedTripAsync(bed);

        var result = await Summary(bed).HandleAsync(new GetTripSummaryReportQuery(tripId), Ct);

        result.IsSuccess.Should().BeTrue(result.Error?.Message);
        var report = result.Value!;
        report.Header.DriverName.Should().Be("Pedro Driver");
        report.Header.VehicleCode.Should().Be("TRK1");
        var a = report.Lines.Single(l => l.ItemId == bed.ItemA.Id);
        a.QtyOrdered.Should().Be(5);
        a.QtyExtra.Should().Be(3);
        a.QtyLoaded.Should().Be(8);
        a.QtyDelivered.Should().Be(5);
        a.QtyReturnedDamaged.Should().Be(1);
        a.QtyDamagedOut.Should().Be(1);
        a.QtyLoadedBack.Should().Be(3);
        var b = report.Lines.Single(l => l.ItemId == bed.ItemB.Id);
        b.QtyVanSold.Should().Be(2);
        b.QtyVariance.Should().Be(-1);
        b.VarianceValue.Should().Be(-10, "one unit short at the trip's cost of 10");
        report.Totals.QtyLoaded.Should().Be(18);
        report.Totals.LoadedValue.Should().Be(180);
        report.Totals.SoldValue.Should().Be(70);
        report.Totals.VarianceValue.Should().Be(-10);
        report.Totals.SalesAmount.Should().Be(672);
    }

    [Fact]
    public async Task Trip_Reports_Are_Only_For_The_Driver_Or_Branch_Viewers()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var tripId = await bed.TripOnTheRoadAsync();
        var denied = Substitute.For<IPermissionService>();
        bed.CurrentUser.UserId.Returns("someone-else");

        var result = await Summary(bed, denied).HandleAsync(new GetTripSummaryReportQuery(tripId), Ct);

        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Trip_Deliveries_List_Sales_And_Voids_In_Series_Order_With_Order_Line_Status()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var tripId = await bed.TripOnTheRoadAsync();
        // Invoice 501 voided at the door; 502 delivers 3 of the order's 5 A on account (no tender).
        (await bed.VoidInvoice().HandleAsync(new VoidRouteInvoiceCommand(tripId, "T1-000501", "Printer jam"), Ct)).IsSuccess.Should().BeTrue();
        var sale = await bed.RecordSale().HandleAsync(bed.Sale(tripId, "T1-000502", cash: 0) with
        {
            Lines = [new RouteSaleLineInput(bed.ItemA.Id, 3, 112, 0, bed.Order.Lines[0].Id)],
            ReceivedByName = "Aling Nena",
            Refusals = [new TripRefusalLineInput(bed.Order.Lines[0].Id, 2, "NO_PAYMENT")],
        }, Ct);
        sale.IsSuccess.Should().BeTrue(sale.Error?.Message);

        var result = await new GetTripDeliveriesReportQueryHandler(bed.Db, bed.Permissions, bed.CurrentUser, IdentityTestFixtures.UserManager(bed.Db))
            .HandleAsync(new GetTripDeliveriesReportQuery(tripId), Ct);

        result.IsSuccess.Should().BeTrue(result.Error?.Message);
        var report = result.Value!;
        report.Stops.Select(s => s.BirOrSeriesNumber).Should().Equal("T1-000501", "T1-000502");
        report.Stops[0].Status.Should().Be("VOID");
        report.Stops[0].VoidReason.Should().Be("Printer jam");
        var stop = report.Stops[1];
        stop.Status.Should().Be("COMPLETED");
        stop.SoNo.Should().Be(bed.Order.SoNo);
        stop.InvoiceNo.Should().NotBeNull();
        stop.DoNo.Should().NotBeNull();
        stop.InvoiceTotal.Should().Be(336);
        stop.AmountCharged.Should().Be(336);
        stop.ReceivedByName.Should().Be("Aling Nena");
        var orderLine = report.OrderLines.Single();
        orderLine.QtyOrdered.Should().Be(5);
        orderLine.QtyDeliveredThisTrip.Should().Be(3);
        orderLine.QtyRefusedThisTrip.Should().Be(2);
        orderLine.QtyOpen.Should().Be(2);
        orderLine.RefusalReasons.Should().Be("Customer can't pay (2)");
        var refusal = report.Refusals.Single();
        refusal.BirOrSeriesNumber.Should().Be("T1-000502", "part of the order was delivered on that invoice");
        refusal.ReasonCode.Should().Be("NO_PAYMENT");
        refusal.Qty.Should().Be(2);
        report.Totals.Invoices.Should().Be(1);
        report.Totals.Voids.Should().Be(1);
        report.Totals.AmountCharged.Should().Be(336);
    }

    [Fact]
    public async Task Cash_Remittance_Splits_Tenders_Between_Sales_And_Collections()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var tripId = await bed.TripOnTheRoadAsync();
        await bed.RecordSale().HandleAsync(bed.Sale(tripId, cash: 0), Ct); // 672 charged
        var invoiceId = (await bed.Db.SalesInvoices.SingleAsync(i => i.Status == "POSTED", Ct)).Id;
        (await bed.RecordCollection().HandleAsync(new RecordTripCollectionCommand(tripId, Guid.NewGuid(), bed.Customer.Id, DateTimeOffset.UtcNow,
            [new TripCollectionAllocation(invoiceId, 300)], [new CustomerPaymentTenderInput(bed.Cash.Id, 300, null, null)]), Ct)).IsSuccess.Should().BeTrue();
        await bed.EndTrip().HandleAsync(new EndTripCommand(tripId, 290, [new(bed.ItemA.Id, 3), new(bed.ItemB.Id, 8)]), Ct);

        var result = await new GetTripCashRemittanceReportQueryHandler(bed.Db, bed.Permissions, bed.CurrentUser, IdentityTestFixtures.UserManager(bed.Db))
            .HandleAsync(new GetTripCashRemittanceReportQuery(tripId), Ct);

        result.IsSuccess.Should().BeTrue(result.Error?.Message);
        var report = result.Value!;
        var cash = report.Tenders.Single();
        cash.MethodCode.Should().Be(bed.Cash.Code);
        cash.FromSales.Should().Be(0);
        cash.FromCollections.Should().Be(300);
        report.SalesInvoiced.Should().Be(672);
        report.SalesCharged.Should().Be(672);
        report.CollectionsTotal.Should().Be(300);
        report.Collections.Single().InvoicesPaid.Should().ContainSingle();
        report.CashExpected.Should().Be(300);
        report.CashRemitted.Should().Be(290);
        report.CashShortOver.Should().Be(-10);
    }

    [Fact]
    public async Task Undelivered_Lists_Open_Order_Lines_And_Flags_An_Open_Trip()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var tripId = await bed.TripOnTheRoadAsync();
        (await bed.RecordSale().HandleAsync(bed.Sale(tripId) with
        {
            Lines = [new RouteSaleLineInput(bed.ItemA.Id, 3, 112, 0, bed.Order.Lines[0].Id)],
            Refusals = [new TripRefusalLineInput(bed.Order.Lines[0].Id, 2, TripRefusalReason.Other, "Shelf full, deliver next week")],
        }, Ct)).IsSuccess.Should().BeTrue();
        var handler = new GetUndeliveredOrdersReportQueryHandler(bed.Db, bed.Permissions);

        var result = await handler.HandleAsync(new GetUndeliveredOrdersReportQuery("br-1", null), Ct);

        result.IsSuccess.Should().BeTrue(result.Error?.Message);
        var line = result.Value!.Lines.Single();
        line.SoNo.Should().Be(bed.Order.SoNo);
        line.QtyDelivered.Should().Be(3);
        line.QtyOpen.Should().Be(2);
        line.OpenAmount.Should().Be(224);
        line.OnOpenTrip.Should().BeTrue();
        line.LastRefusalReason.Should().Be("Other (explain in notes) — Shelf full, deliver next week");
        (await handler.HandleAsync(new GetUndeliveredOrdersReportQuery(null, Guid.NewGuid()), Ct)).Value!.Lines.Should().BeEmpty();
    }

    [Fact]
    public async Task Route_Performance_Groups_Closed_Trips_With_Fill_Rate_Returns_And_Shortages()
    {
        await using var bed = await TripTestBed.CreateAsync();
        await ClosedTripAsync(bed);
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);
        var handler = new GetRoutePerformanceReportQueryHandler(bed.Db, bed.Permissions, IdentityTestFixtures.UserManager(bed.Db));

        var byDriver = await handler.HandleAsync(new GetRoutePerformanceReportQuery(today, today, null, "driver"), Ct);

        byDriver.IsSuccess.Should().BeTrue(byDriver.Error?.Message);
        var row = byDriver.Value!.Rows.Single();
        row.Label.Should().Be("Pedro Driver");
        row.Trips.Should().Be(1);
        row.FillRatePct.Should().Be(100);
        row.QtyReturned.Should().Be(2);
        row.ReturnsPct.Should().Be(Math.Round(2m / 7 * 100, 2));
        row.SalesAmount.Should().Be(672);
        row.VarianceValue.Should().Be(-10);
        row.CashShortOver.Should().Be(-22);
        row.ShortageCharged.Should().Be(32);
        byDriver.Value.Total.Trips.Should().Be(1);

        (await handler.HandleAsync(new GetRoutePerformanceReportQuery(today, today, null, "route"), Ct)).Value!.Rows.Single().Label.Should().Be("(no route)");
        (await handler.HandleAsync(new GetRoutePerformanceReportQuery(today.AddDays(1), today.AddDays(2), null, null), Ct)).Value!.Rows.Should().BeEmpty();
        (await handler.HandleAsync(new GetRoutePerformanceReportQuery(today, today, null, "weekday"), Ct)).Error!.Code.Should().Be("RoutePerformance.InvalidGroupBy");
    }

    [Fact]
    public async Task Sales_Book_Can_Be_Narrowed_To_One_Truck_Terminal()
    {
        await using var bed = await TripTestBed.CreateAsync();
        var tripId = await bed.TripOnTheRoadAsync();
        await bed.RecordSale().HandleAsync(bed.Sale(tripId), Ct);
        var handler = new GetSalesBookReportQueryHandler(bed.Db, bed.Permissions);

        (await handler.HandleAsync(new GetSalesBookReportQuery("br-1", bed.TerminalId), Ct)).Value!.Rows
            .Should().ContainSingle(r => r.BirOrSeriesNumber == "T1-000501");
        (await handler.HandleAsync(new GetSalesBookReportQuery("br-1", Guid.NewGuid()), Ct)).Value!.Rows.Should().BeEmpty();
    }
}
