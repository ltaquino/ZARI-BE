using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZARI.Application.Features.RouteSales.Reports.DriverShortages;
using static ZARI.Api.Reporting.TripReportPdf;

namespace ZARI.Api.Reporting;

public sealed class DriverShortagesDocument(DriverShortagesReportResponse report, string subtitle) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    private static string SettlementLabel(string s) => s switch
    {
        "PAYROLL" => "Payroll",
        "DRIVER_PAYS" => "Driver pays",
        "WRITE_OFF" => "Written off",
        _ => s,
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(25);
            page.DefaultTextStyle(x => x.FontSize(8));
            page.Header().Column(col =>
            {
                col.Item().Text("Driver Shortages").FontSize(16).Bold();
                col.Item().Text(subtitle);
            });
            page.Content().PaddingTop(10).Column(col =>
            {
                col.Item().Text("By driver").FontSize(11).Bold();
                col.Item().PaddingTop(4).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(4);
                        for (var i = 0; i < 6; i++) c.RelativeColumn(1.6f);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("Driver").Bold();
                        foreach (var label in new[] { "Trips", "Shortage", "Charged", "Written off", "Paid", "Still owed" })
                            h.Cell().AlignRight().Text(label).Bold();
                    });
                    foreach (var d in report.Drivers)
                    {
                        table.Cell().Text(d.DriverName);
                        table.Cell().AlignRight().Text(d.Trips.ToString());
                        foreach (var v in new[] { d.Shortage, d.Charged, d.WrittenOff, d.Paid, d.Balance })
                            table.Cell().AlignRight().Text(Money(v));
                    }
                    table.Cell().Text("TOTAL").Bold();
                    table.Cell().AlignRight().Text(report.Rows.Count.ToString()).Bold();
                    foreach (var v in new[] { report.TotalShortage, report.TotalCharged, report.TotalWrittenOff, report.TotalPaid, report.TotalBalance })
                        table.Cell().AlignRight().Text(Money(v)).Bold();
                });

                col.Item().PaddingTop(14).Text("By trip").FontSize(11).Bold();
                col.Item().PaddingTop(4).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3.5f); // Driver
                        c.RelativeColumn(2.4f); // Trip
                        c.RelativeColumn(1.8f); // Date
                        c.RelativeColumn(2);    // Truck
                        c.RelativeColumn(2);    // Settlement
                        for (var i = 0; i < 5; i++) c.RelativeColumn(1.6f);
                    });
                    table.Header(h =>
                    {
                        foreach (var label in new[] { "Driver", "Trip", "Date", "Truck", "Settled by" })
                            h.Cell().Text(label).Bold();
                        foreach (var label in new[] { "Shortage", "Charged", "Written off", "Paid", "Still owed" })
                            h.Cell().AlignRight().Text(label).Bold();
                    });
                    foreach (var r in report.Rows)
                    {
                        table.Cell().Text(r.DriverName);
                        table.Cell().Text(r.TripNo);
                        table.Cell().Text($"{r.TripDate:yyyy-MM-dd}");
                        table.Cell().Text(r.VehicleCode);
                        table.Cell().Text(SettlementLabel(r.Settlement));
                        foreach (var v in new[] { r.Shortage, r.Charged, r.WrittenOff, r.Paid, r.Balance })
                            table.Cell().AlignRight().Text(Money(v));
                    }
                });
            });
            page.Footer().PageNumbers();
        });
    }
}
