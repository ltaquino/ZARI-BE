using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZARI.Application.Features.Purchasing.Reports.ApAging;

namespace ZARI.Api.Reporting;

public sealed class ApAgingDocument(ApAgingReportResponse report) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(9));
            page.Header().Text("AP Aging Report").FontSize(16).Bold();
            page.Content().Column(column =>
            {
                column.Item().PaddingTop(4).Text(
                    $"Total Outstanding: {report.TotalOutstanding:N2}   |   Current: {report.Current:N2}   |   1-30: {report.Days1To30:N2}   |   31-60: {report.Days31To60:N2}   |   61-90: {report.Days61To90:N2}   |   90+: {report.Days90Plus:N2}")
                    .FontSize(9).Bold();

                foreach (var group in report.Groups)
                {
                    column.Item().PaddingTop(10).Text($"{group.SupplierCode} — {group.SupplierName} — {group.GroupTotal:N2}").Bold();
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(2);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Text("Invoice No.").Bold();
                            h.Cell().Text("Supplier Inv. No.").Bold();
                            h.Cell().Text("Branch").Bold();
                            h.Cell().Text("Due Date").Bold();
                            h.Cell().Text("Bucket").Bold();
                            h.Cell().AlignRight().Text("Outstanding").Bold();
                        });
                        foreach (var inv in group.Invoices)
                        {
                            table.Cell().Text(inv.InvoiceNo);
                            table.Cell().Text(inv.SupplierInvoiceNo);
                            table.Cell().Text(inv.BranchId);
                            table.Cell().Text(inv.DueDate.ToString("yyyy-MM-dd"));
                            table.Cell().Text($"{inv.Bucket} ({inv.DaysOverdue}d)");
                            table.Cell().AlignRight().Text(inv.Outstanding.ToString("N2"));
                        }
                    });
                }
            });
            page.Footer().AlignCenter().Text(x =>
            {
                x.CurrentPageNumber();
                x.Span(" / ");
                x.TotalPages();
            });
        });
    }
}
