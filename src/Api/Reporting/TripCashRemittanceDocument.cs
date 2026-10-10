using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZARI.Application.Features.RouteSales.Reports.TripCashRemittance;
using static ZARI.Api.Reporting.TripReportPdf;

namespace ZARI.Api.Reporting;

public sealed class TripCashRemittanceDocument(TripCashRemittanceReportResponse report) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(9));
            page.Header().TripHeader("Cash Remittance", report.Header);
            page.Content().PaddingTop(10).Column(col =>
            {
                col.Item().Text("Received by tender").FontSize(11).Bold();
                col.Item().PaddingTop(4).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(4);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("Tender").Bold();
                        h.Cell().AlignRight().Text("Sales").Bold();
                        h.Cell().AlignRight().Text("Collections").Bold();
                        h.Cell().AlignRight().Text("Total").Bold();
                    });
                    foreach (var t in report.Tenders)
                    {
                        table.Cell().Text(t.MethodName);
                        table.Cell().AlignRight().Text(Money(t.FromSales));
                        table.Cell().AlignRight().Text(Money(t.FromCollections));
                        table.Cell().AlignRight().Text(Money(t.Total));
                    }
                    table.Cell().Text("TOTAL").Bold();
                    table.Cell().AlignRight().Text(Money(report.Tenders.Sum(t => t.FromSales))).Bold();
                    table.Cell().AlignRight().Text(Money(report.Tenders.Sum(t => t.FromCollections))).Bold();
                    table.Cell().AlignRight().Text(Money(report.TotalReceived)).Bold();
                });

                col.Item().PaddingTop(12).Text("Settlement").FontSize(11).Bold();
                col.Item().PaddingTop(4).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(5);
                        c.RelativeColumn(2);
                    });
                    void Row(string label, string value, bool bold = false)
                    {
                        var l = table.Cell().Text(label);
                        var v = table.Cell().AlignRight().Text(value);
                        if (bold) { l.Bold(); v.Bold(); }
                    }
                    Row("Sales invoiced", Money(report.SalesInvoiced));
                    Row("Paid at the stop", Money(report.SalesPaid));
                    Row("Left on account (charge sales)", Money(report.SalesCharged));
                    Row("Collections of older invoices", Money(report.CollectionsTotal));
                    Row("Cash expected from the driver", Money(report.CashExpected), true);
                    Row("Cash remitted", Money(report.CashRemitted), true);
                    Row("Cash short (−) / over (+)", Money(report.CashShortOver), true);
                    Row("Shortage found at close (stock + cash)", Money(report.ShortageAmount));
                    Row("Shortage settled by", report.ShortageSettlement switch
                    {
                        "PAYROLL" => "Payroll deduction",
                        "DRIVER_PAYS" => "Driver pays",
                        "WRITE_OFF" => "Written off (recorded only)",
                        _ => "-",
                    });
                    Row("Shortage charged to the driver (stock + cash)", Money(report.ShortageCharged));
                    if (report.ShortageSettlement == "DRIVER_PAYS")
                    {
                        Row("Paid in by the driver", Money(report.ShortagePaid));
                        Row("Driver still owes", Money(report.ShortageBalance), true);
                    }
                });

                if (report.ShortagePayments.Count > 0)
                {
                    col.Item().PaddingTop(12).Text("Driver's shortage payments").FontSize(11).Bold();
                    foreach (var p in report.ShortagePayments)
                        col.Item().Text($"{p.PaidAt:yyyy-MM-dd}   {p.PaymentMethodName}   {Money(p.Amount)}   {p.ReferenceNo}");
                }

                if (report.Collections.Count > 0)
                {
                    col.Item().PaddingTop(12).Text("Collections").FontSize(11).Bold();
                    col.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(4);
                            c.RelativeColumn(2.5f);
                            c.RelativeColumn(4);
                            c.RelativeColumn(2);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Text("Time").Bold();
                            h.Cell().Text("Customer").Bold();
                            h.Cell().Text("Payment No.").Bold();
                            h.Cell().Text("Invoices paid").Bold();
                            h.Cell().AlignRight().Text("Amount").Bold();
                        });
                        foreach (var c in report.Collections)
                        {
                            table.Cell().Text($"{c.CollectedAt:MM-dd HH:mm}");
                            table.Cell().Text(c.CustomerName);
                            table.Cell().Text(c.PaymentNo ?? c.Status);
                            table.Cell().Text(string.Join(", ", c.InvoicesPaid));
                            table.Cell().AlignRight().Text(Money(c.Amount));
                        }
                    });
                }

                if (report.Voids.Count > 0)
                {
                    col.Item().PaddingTop(12).Text("Voided invoice numbers").FontSize(11).Bold();
                    foreach (var v in report.Voids)
                        col.Item().Text($"{v.BirOrSeriesNumber}   {v.VoidedAt:yyyy-MM-dd HH:mm}   {v.Reason}");
                }

                col.Item().PaddingTop(30).Row(row =>
                {
                    row.RelativeItem().Column(c => { c.Item().Text("______________________________"); c.Item().Text("Driver"); });
                    row.RelativeItem().Column(c => { c.Item().Text("______________________________"); c.Item().Text("Received by (cashier)"); });
                });
            });
            page.Footer().PageNumbers();
        });
    }
}
