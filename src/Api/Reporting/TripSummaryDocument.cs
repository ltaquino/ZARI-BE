using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZARI.Application.Features.RouteSales.Reports.TripSummary;
using static ZARI.Api.Reporting.TripReportPdf;

namespace ZARI.Api.Reporting;

public sealed class TripSummaryDocument(TripSummaryReportResponse report) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(25);
            page.DefaultTextStyle(x => x.FontSize(8));
            page.Header().TripHeader("Trip Summary", report.Header);
            page.Content().PaddingTop(10).Column(col =>
            {
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(4); // Item
                        for (var i = 0; i < 13; i++) c.RelativeColumn(1.25f);
                        c.RelativeColumn(1.6f); // Unit cost
                        c.RelativeColumn(1.8f); // Variance value
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("Item").Bold();
                        foreach (var label in new[] { "Ordered", "Extra", "Loaded", "Load check", "Delivered", "Van sold", "Refused", "Ret. good", "Ret. dmg",
                                     "Counted", "To WH", "To Dmg", "Variance", "Unit cost", "Var. value" })
                            h.Cell().AlignRight().Text(label).Bold();
                    });
                    foreach (var l in report.Lines)
                    {
                        table.Cell().Text($"{l.ItemCode} {l.ItemName}");
                        foreach (var v in new[] { Qty(l.QtyOrdered), Qty(l.QtyExtra), Qty(l.QtyLoaded), Qty(l.QtyLoadCheck), Qty(l.QtyDelivered),
                                     Qty(l.QtyVanSold), Qty(l.QtyRefused), Qty(l.QtyReturnedGood), Qty(l.QtyReturnedDamaged), Qty(l.QtyCountedAtEnd), Qty(l.QtyLoadedBack),
                                     Qty(l.QtyDamagedOut), Qty(l.QtyVariance), Money(l.UnitCost), Money(l.VarianceValue) })
                            table.Cell().AlignRight().Text(v);
                    }
                    var t = report.Totals;
                    table.Cell().Text("TOTAL").Bold();
                    foreach (var v in new[] { Qty(t.QtyOrdered), Qty(t.QtyExtra), Qty(t.QtyLoaded), "", Qty(t.QtyDelivered), Qty(t.QtyVanSold),
                                 Qty(t.QtyRefused), Qty(t.QtyReturnedGood), Qty(t.QtyReturnedDamaged), "", Qty(t.QtyLoadedBack), Qty(t.QtyDamagedOut), Qty(t.QtyVariance),
                                 "", Money(t.VarianceValue) })
                        table.Cell().AlignRight().Text(v).Bold();
                });
                col.Item().PaddingTop(10).Text(text =>
                {
                    text.Span("Loaded value (at cost): ").Bold(); text.Span($"{Money(report.Totals.LoadedValue)}   ");
                    text.Span("Sold value (at cost): ").Bold(); text.Span($"{Money(report.Totals.SoldValue)}   ");
                    text.Span("Sales invoiced: ").Bold(); text.Span(Money(report.Totals.SalesAmount));
                });
            });
            page.Footer().PageNumbers();
        });
    }
}
