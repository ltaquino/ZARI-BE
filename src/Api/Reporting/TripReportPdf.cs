using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using ZARI.Application.Features.RouteSales.Reports;

namespace ZARI.Api.Reporting;

/// <summary>Shared pieces of the route-sales report PDFs: the trip heading block and the page footer.</summary>
internal static class TripReportPdf
{
    public static void TripHeader(this IContainer container, string title, TripReportHeader h)
    {
        container.Column(col =>
        {
            col.Item().Text($"{title} — {h.TripNo}").FontSize(16).Bold();
            col.Item().PaddingTop(4).Text(text =>
            {
                text.Span("Date: ").Bold(); text.Span($"{h.TripDate:yyyy-MM-dd}   ");
                text.Span("Branch: ").Bold(); text.Span($"{h.BranchId}   ");
                text.Span("Status: ").Bold(); text.Span(h.Status);
            });
            col.Item().Text(text =>
            {
                text.Span("Truck: ").Bold(); text.Span($"{h.VehicleCode} ({h.PlateNo})   ");
                text.Span("Route: ").Bold(); text.Span($"{(h.RouteName is null ? "-" : $"{h.RouteCode} {h.RouteName}")}   ");
                text.Span("Loaded from: ").Bold(); text.Span(h.SourceWarehouseName);
            });
            col.Item().Text(text =>
            {
                text.Span("Driver: ").Bold(); text.Span($"{h.DriverName}   ");
                text.Span("Helpers: ").Bold(); text.Span($"{h.HelperNames ?? "-"}   ");
                text.Span("Closed: ").Bold(); text.Span(h.ClosedAt is { } c ? $"{c:yyyy-MM-dd HH:mm}" : "-");
            });
        });
    }

    public static void PageNumbers(this IContainer container) =>
        container.AlignCenter().Text(x =>
        {
            x.CurrentPageNumber();
            x.Span(" / ");
            x.TotalPages();
        });

    public static string Qty(decimal value) => value.ToString("#,0.####");
    public static string Qty(decimal? value) => value is { } v ? Qty(v) : "-";
    public static string Money(decimal value) => value.ToString("N2");
    public static string Money(decimal? value) => value is { } v ? Money(v) : "-";
}
