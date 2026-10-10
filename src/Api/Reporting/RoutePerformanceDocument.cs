using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZARI.Application.Features.RouteSales.Reports.RoutePerformance;
using static ZARI.Api.Reporting.TripReportPdf;

namespace ZARI.Api.Reporting;

public sealed class RoutePerformanceDocument(RoutePerformanceReportResponse report) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(25);
            page.DefaultTextStyle(x => x.FontSize(8));
            page.Header().Column(col =>
            {
                col.Item().Text("Route Performance").FontSize(16).Bold();
                col.Item().Text($"Closed trips {report.From:yyyy-MM-dd} to {report.To:yyyy-MM-dd}, by {report.GroupBy}");
            });
            page.Content().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(4.5f);
                    for (var i = 0; i < 14; i++) c.RelativeColumn(1.4f);
                });
                table.Header(h =>
                {
                    h.Cell().Text(char.ToUpperInvariant(report.GroupBy[0]) + report.GroupBy[1..]).Bold();
                    foreach (var label in new[] { "Trips", "Ordered", "Delivered", "Fill %", "Van sold", "Not deliv.", "Returned", "Returns %", "Sales",
                                 "Var. qty", "Var. value", "Cash +/−", "Shortage", "Charged" })
                        h.Cell().AlignRight().Text(label).Bold();
                });
                void Row(RoutePerformanceRow r, bool bold)
                {
                    var cells = new[] { r.Trips.ToString(), Qty(r.QtyOrdered), Qty(r.QtyDelivered), Pct(r.FillRatePct), Qty(r.QtyVanSold),
                        Qty(r.QtyRefused), Qty(r.QtyReturned), Pct(r.ReturnsPct), Money(r.SalesAmount), Qty(r.QtyVariance), Money(r.VarianceValue),
                        Money(r.CashShortOver), Money(r.ShortageAmount), Money(r.ShortageCharged) };
                    var label = table.Cell().Text(r.Label);
                    if (bold) label.Bold();
                    foreach (var v in cells)
                    {
                        var cell = table.Cell().AlignRight().Text(v);
                        if (bold) cell.Bold();
                    }
                }
                foreach (var r in report.Rows) Row(r, false);
                Row(report.Total, true);
            });
            page.Footer().PageNumbers();
        });
    }

    private static string Pct(decimal? value) => value is { } v ? $"{v:0.##}%" : "-";
}
