using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using ZARI.Api.Extensions;
using ZARI.Api.Reporting;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.Reports.DriverShortages;
using ZARI.Application.Features.RouteSales.Reports.RoutePerformance;
using ZARI.Application.Features.RouteSales.Reports.TripCashRemittance;
using ZARI.Application.Features.RouteSales.Reports.TripDeliveries;
using ZARI.Application.Features.RouteSales.Reports.TripSummary;
using ZARI.Application.Features.RouteSales.Reports.UndeliveredOrders;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

/// <summary>
/// Route Sales monitoring reports (RouteSalesContext.md §6), each a JSON route for the screen and a
/// sibling /pdf for printing. The truck's own Z-Reading is /api/pos-closing/z-reading?posTerminalId=
/// and its sales book /api/sales/reports/sales-book?posTerminalId= (step 1 / this step).
/// </summary>
public static class RouteSalesReportEndpoints
{
    public static void MapRouteSalesReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/route-sales/reports").WithTags("RouteSalesReports").WithGroupName("RouteSales").RequireAuthorization();

        group.MapGet("/trips/{tripId:guid}/summary", TripSummary).WithName("GetTripSummaryReport")
            .WithSummary("Per item: ordered, loaded, delivered, van sold, returned, back to warehouse, damaged, variance");
        group.MapGet("/trips/{tripId:guid}/summary/pdf", TripSummaryPdf).WithName("GetTripSummaryReportPdf").WithSummary("Trip Summary, as a PDF");

        group.MapGet("/trips/{tripId:guid}/deliveries", TripDeliveries).WithName("GetTripDeliveriesReport")
            .WithSummary("Every invoice number the truck used (sales + voids, with POD) and each order line's delivery status");
        group.MapGet("/trips/{tripId:guid}/deliveries/pdf", TripDeliveriesPdf).WithName("GetTripDeliveriesReportPdf").WithSummary("Trip Deliveries, as a PDF");

        group.MapGet("/trips/{tripId:guid}/cash-remittance", TripCash).WithName("GetTripCashRemittanceReport")
            .WithSummary("Takings by tender, collections, voids, and cash expected vs remitted");
        group.MapGet("/trips/{tripId:guid}/cash-remittance/pdf", TripCashPdf).WithName("GetTripCashRemittanceReportPdf").WithSummary("Cash Remittance, as a PDF");

        group.MapGet("/undelivered", Undelivered).WithName("GetUndeliveredOrdersReport")
            .WithSummary("Sales order lines that went out on a trip and are still not fully delivered");
        group.MapGet("/undelivered/pdf", UndeliveredPdf).WithName("GetUndeliveredOrdersReportPdf").WithSummary("Undelivered Orders, as a PDF");

        group.MapGet("/performance", Performance).WithName("GetRoutePerformanceReport")
            .WithSummary("Closed trips in a date range by route, truck or driver: sales, fill rate, returns, variance, cash short/over");
        group.MapGet("/driver-shortages", DriverShortages).WithName("GetDriverShortagesReport")
            .WithSummary("Every trip shortage by driver and how it was settled (payroll / driver pays / written off); from/to on trip date; outstandingOnly=true: driver-pays balances only");
        group.MapGet("/driver-shortages/pdf", DriverShortagesPdf).WithName("GetDriverShortagesReportPdf").WithSummary("Driver Shortages, as a PDF");

        group.MapGet("/performance/pdf", PerformancePdf).WithName("GetRoutePerformanceReportPdf").WithSummary("Route Performance, as a PDF");
    }

    private static IResult Json<T>(Result<T> result) => result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();

    private static IResult Pdf<T>(Result<T> result, Func<T, IDocument> document, string fileName) =>
        result.IsSuccess ? Results.File(document(result.Value!).GeneratePdf(), "application/pdf", fileName) : result.ToProblemDetails();

    private static async Task<IResult> TripSummary(Guid tripId, IQueryHandler<GetTripSummaryReportQuery, Result<TripSummaryReportResponse>> handler, CancellationToken ct) =>
        Json(await handler.HandleAsync(new GetTripSummaryReportQuery(tripId), ct));

    private static async Task<IResult> TripSummaryPdf(Guid tripId, IQueryHandler<GetTripSummaryReportQuery, Result<TripSummaryReportResponse>> handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetTripSummaryReportQuery(tripId), ct);
        return Pdf(result, r => new TripSummaryDocument(r), $"trip-summary-{result.Value?.Header.TripNo}.pdf");
    }

    private static async Task<IResult> TripDeliveries(Guid tripId, IQueryHandler<GetTripDeliveriesReportQuery, Result<TripDeliveriesReportResponse>> handler, CancellationToken ct) =>
        Json(await handler.HandleAsync(new GetTripDeliveriesReportQuery(tripId), ct));

    private static async Task<IResult> TripDeliveriesPdf(Guid tripId, IQueryHandler<GetTripDeliveriesReportQuery, Result<TripDeliveriesReportResponse>> handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetTripDeliveriesReportQuery(tripId), ct);
        return Pdf(result, r => new TripDeliveriesDocument(r), $"trip-deliveries-{result.Value?.Header.TripNo}.pdf");
    }

    private static async Task<IResult> TripCash(Guid tripId, IQueryHandler<GetTripCashRemittanceReportQuery, Result<TripCashRemittanceReportResponse>> handler, CancellationToken ct) =>
        Json(await handler.HandleAsync(new GetTripCashRemittanceReportQuery(tripId), ct));

    private static async Task<IResult> TripCashPdf(Guid tripId, IQueryHandler<GetTripCashRemittanceReportQuery, Result<TripCashRemittanceReportResponse>> handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetTripCashRemittanceReportQuery(tripId), ct);
        return Pdf(result, r => new TripCashRemittanceDocument(r), $"trip-cash-remittance-{result.Value?.Header.TripNo}.pdf");
    }

    private static async Task<IResult> Undelivered(string? branchId, Guid? tripId,
        IQueryHandler<GetUndeliveredOrdersReportQuery, Result<UndeliveredOrdersReportResponse>> handler, CancellationToken ct) =>
        Json(await handler.HandleAsync(new GetUndeliveredOrdersReportQuery(branchId, tripId), ct));

    private static async Task<IResult> UndeliveredPdf(string? branchId, Guid? tripId,
        IQueryHandler<GetUndeliveredOrdersReportQuery, Result<UndeliveredOrdersReportResponse>> handler, CancellationToken ct) =>
        Pdf(await handler.HandleAsync(new GetUndeliveredOrdersReportQuery(branchId, tripId), ct), r => new UndeliveredOrdersDocument(r), "undelivered-orders.pdf");

    private static async Task<IResult> Performance(DateOnly from, DateOnly to, string? branchId, string? groupBy,
        IQueryHandler<GetRoutePerformanceReportQuery, Result<RoutePerformanceReportResponse>> handler, CancellationToken ct) =>
        Json(await handler.HandleAsync(new GetRoutePerformanceReportQuery(from, to, branchId, groupBy), ct));

    private static async Task<IResult> PerformancePdf(DateOnly from, DateOnly to, string? branchId, string? groupBy,
        IQueryHandler<GetRoutePerformanceReportQuery, Result<RoutePerformanceReportResponse>> handler, CancellationToken ct) =>
        Pdf(await handler.HandleAsync(new GetRoutePerformanceReportQuery(from, to, branchId, groupBy), ct), r => new RoutePerformanceDocument(r), "route-performance.pdf");

    private static async Task<IResult> DriverShortages(string? branchId, bool? outstandingOnly, DateOnly? from, DateOnly? to,
        IQueryHandler<GetDriverShortagesReportQuery, Result<DriverShortagesReportResponse>> handler, CancellationToken ct) =>
        Json(await handler.HandleAsync(new GetDriverShortagesReportQuery(branchId, outstandingOnly ?? false, from, to), ct));

    private static async Task<IResult> DriverShortagesPdf(string? branchId, bool? outstandingOnly, DateOnly? from, DateOnly? to,
        IQueryHandler<GetDriverShortagesReportQuery, Result<DriverShortagesReportResponse>> handler, CancellationToken ct)
    {
        var subtitle = string.Join(" · ", new[]
        {
            outstandingOnly == true ? "Still owed by drivers" : "All shortages (payroll, driver pays, written off)",
            from is null && to is null ? null : $"Trips {from:yyyy-MM-dd} to {to:yyyy-MM-dd}",
            branchId is null ? null : $"Branch {branchId}",
        }.OfType<string>());
        return Pdf(await handler.HandleAsync(new GetDriverShortagesReportQuery(branchId, outstandingOnly ?? false, from, to), ct),
            r => new DriverShortagesDocument(r, subtitle), "driver-shortages.pdf");
    }
}
