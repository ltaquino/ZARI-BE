using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZARI.Application.Features.RouteSales.Reports.UndeliveredOrders;
using static ZARI.Api.Reporting.TripReportPdf;

namespace ZARI.Api.Reporting;

public sealed class UndeliveredOrdersDocument(UndeliveredOrdersReportResponse report) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(25);
            page.DefaultTextStyle(x => x.FontSize(8));
            page.Header().Text("Undelivered Orders (back-orders)").FontSize(16).Bold();
            page.Content().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2);    // SO
                    c.RelativeColumn(1.6f); // Order date
                    c.RelativeColumn(3.5f); // Customer
                    c.RelativeColumn(4);    // Item
                    for (var i = 0; i < 4; i++) c.RelativeColumn(1.5f);
                    c.RelativeColumn(2.2f); // Last trip
                    c.RelativeColumn(2.4f); // Trip status
                    c.RelativeColumn(4);    // Last reason
                });
                table.Header(h =>
                {
                    foreach (var label in new[] { "SO No.", "Ordered on", "Customer", "Item" })
                        h.Cell().Text(label).Bold();
                    foreach (var label in new[] { "Ordered", "Delivered", "Open", "Open amount" })
                        h.Cell().AlignRight().Text(label).Bold();
                    h.Cell().Text("Last trip").Bold();
                    h.Cell().Text("Trip status").Bold();
                    h.Cell().Text("Last reason not delivered").Bold();
                });
                foreach (var l in report.Lines)
                {
                    table.Cell().Text(l.SoNo);
                    table.Cell().Text($"{l.OrderDate:yyyy-MM-dd}");
                    table.Cell().Text(l.CustomerName);
                    table.Cell().Text($"{l.ItemCode} {l.ItemName}");
                    table.Cell().AlignRight().Text(Qty(l.QtyOrdered));
                    table.Cell().AlignRight().Text(Qty(l.QtyDelivered));
                    table.Cell().AlignRight().Text(Qty(l.QtyOpen));
                    table.Cell().AlignRight().Text(Money(l.OpenAmount));
                    table.Cell().Text(l.LastTripNo);
                    table.Cell().Text(l.OnOpenTrip ? $"{l.LastTripStatus} (still out)" : l.LastTripStatus);
                    table.Cell().Text(l.LastRefusalReason ?? "-");
                }
                table.Cell().ColumnSpan(6).Text("TOTAL").Bold();
                table.Cell().AlignRight().Text(Qty(report.TotalQtyOpen)).Bold();
                table.Cell().AlignRight().Text(Money(report.TotalOpenAmount)).Bold();
                table.Cell().ColumnSpan(3).Text("");
            });
            page.Footer().PageNumbers();
        });
    }
}
