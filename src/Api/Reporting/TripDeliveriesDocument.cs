using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZARI.Application.Features.RouteSales.Reports.TripDeliveries;
using static ZARI.Api.Reporting.TripReportPdf;

namespace ZARI.Api.Reporting;

public sealed class TripDeliveriesDocument(TripDeliveriesReportResponse report) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(25);
            page.DefaultTextStyle(x => x.FontSize(8));
            page.Header().TripHeader("Trip Deliveries", report.Header);
            page.Content().PaddingTop(10).Column(col =>
            {
                col.Item().Text("Invoices issued").FontSize(11).Bold();
                col.Item().PaddingTop(4).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2.2f); // Invoice no.
                        c.RelativeColumn(1.6f); // Time
                        c.RelativeColumn(1.4f); // Status
                        c.RelativeColumn(3.2f); // Customer
                        c.RelativeColumn(2);    // SO
                        c.RelativeColumn(2);    // DR
                        c.RelativeColumn(1.6f); // Total
                        c.RelativeColumn(1.6f); // Paid
                        c.RelativeColumn(1.6f); // Charged
                        c.RelativeColumn(3);    // Received by / void reason
                        c.RelativeColumn(1.2f); // POD
                    });
                    table.Header(h =>
                    {
                        foreach (var label in new[] { "Invoice No.", "Time", "Status", "Customer", "SO No.", "DR No." })
                            h.Cell().Text(label).Bold();
                        foreach (var label in new[] { "Total", "Paid", "Charged" })
                            h.Cell().AlignRight().Text(label).Bold();
                        h.Cell().PaddingLeft(6).Text("Received by / void reason").Bold();
                        h.Cell().Text("POD").Bold();
                    });
                    foreach (var s in report.Stops)
                    {
                        table.Cell().Text(s.BirOrSeriesNumber);
                        table.Cell().Text($"{s.SoldAt:MM-dd HH:mm}");
                        table.Cell().Text(s.Status);
                        table.Cell().Text(s.CustomerName ?? "-");
                        table.Cell().Text(s.SoNo ?? "-");
                        table.Cell().Text(s.DoNo ?? "-");
                        table.Cell().AlignRight().Text(Money(s.InvoiceTotal));
                        table.Cell().AlignRight().Text(Money(s.AmountPaid));
                        table.Cell().AlignRight().Text(Money(s.AmountCharged));
                        table.Cell().PaddingLeft(6).Text(s.Status == "VOID" ? s.VoidReason ?? "-" : s.ReceivedByName ?? "-");
                        table.Cell().Text(string.Join(" ", new[] { s.SignatureUrl is null ? null : "sig", s.PhotoUrl is null ? null : "photo" }.OfType<string>()));
                    }
                    var t = report.Totals;
                    table.Cell().ColumnSpan(6).Text($"TOTAL — {t.Invoices} invoice(s), {t.Voids} void(s){(t.Pending > 0 ? $", {t.Pending} pending" : "")}").Bold();
                    table.Cell().AlignRight().Text(Money(t.InvoiceTotal)).Bold();
                    table.Cell().AlignRight().Text(Money(t.AmountPaid)).Bold();
                    table.Cell().AlignRight().Text(Money(t.AmountCharged)).Bold();
                    table.Cell().ColumnSpan(2).Text("");
                });

                col.Item().PaddingTop(14).Text("Sales orders on this trip").FontSize(11).Bold();
                col.Item().PaddingTop(4).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2);   // SO
                        c.RelativeColumn(3.5f); // Customer
                        c.RelativeColumn(4);   // Item
                        for (var i = 0; i < 5; i++) c.RelativeColumn(1.5f);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("SO No.").Bold();
                        h.Cell().Text("Customer").Bold();
                        h.Cell().Text("Item").Bold();
                        foreach (var label in new[] { "Ordered", "This trip", "Elsewhere", "Not deliv.", "Still open" })
                            h.Cell().AlignRight().Text(label).Bold();
                    });
                    foreach (var l in report.OrderLines)
                    {
                        table.Cell().Text(l.SoNo);
                        table.Cell().Text(l.CustomerName);
                        table.Cell().Text($"{l.ItemCode} {l.ItemName}");
                        table.Cell().AlignRight().Text(Qty(l.QtyOrdered));
                        table.Cell().AlignRight().Text(Qty(l.QtyDeliveredThisTrip));
                        table.Cell().AlignRight().Text(Qty(l.QtyDeliveredOtherwise));
                        table.Cell().AlignRight().Text(Qty(l.QtyRefusedThisTrip));
                        table.Cell().AlignRight().Text(Qty(l.QtyOpen));
                    }
                });

                if (report.Refusals.Count > 0)
                {
                    col.Item().PaddingTop(14).Text("Not delivered — refused or only partly received").FontSize(11).Bold();
                    col.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.6f); // Time
                            c.RelativeColumn(3);    // Customer
                            c.RelativeColumn(2);    // SO
                            c.RelativeColumn(2.2f); // Invoice
                            c.RelativeColumn(3.5f); // Item
                            c.RelativeColumn(1.2f); // Qty
                            c.RelativeColumn(5);    // Reason
                        });
                        table.Header(h =>
                        {
                            foreach (var label in new[] { "Time", "Customer", "SO No.", "Invoice No.", "Item" })
                                h.Cell().Text(label).Bold();
                            h.Cell().AlignRight().Text("Qty").Bold();
                            h.Cell().PaddingLeft(6).Text("Reason").Bold();
                        });
                        foreach (var r in report.Refusals)
                        {
                            table.Cell().Text($"{r.RefusedAt:MM-dd HH:mm}");
                            table.Cell().Text(r.CustomerName);
                            table.Cell().Text(r.SoNo);
                            table.Cell().Text(r.BirOrSeriesNumber ?? "(none accepted)");
                            table.Cell().Text($"{r.ItemCode} {r.ItemName}");
                            table.Cell().AlignRight().Text(Qty(r.Qty));
                            table.Cell().PaddingLeft(6).Text(r.Notes is null ? r.Reason : $"{r.Reason} — {r.Notes}");
                        }
                    });
                }
            });
            page.Footer().PageNumbers();
        });
    }
}
