using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZARI.Application.Features.Sales.Reports.CashReceiptsBook;

namespace ZARI.Api.Reporting;

public sealed class CashReceiptsBookDocument(CashReceiptsBookReportResponse report) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(9));
            page.Header().Text("Cash Receipts Book").FontSize(16).Bold();
            page.Content().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2); // Date
                    c.RelativeColumn(3); // Payment No.
                    c.RelativeColumn(4); // Customer
                    c.RelativeColumn(2); // Method
                    c.RelativeColumn(2); // Branch
                    c.RelativeColumn(3); // Ref No.
                    c.RelativeColumn(2); // Status
                    c.RelativeColumn(2); // Amount
                    c.RelativeColumn(2); // Running Total
                });
                table.Header(h =>
                {
                    h.Cell().Text("Date").Bold();
                    h.Cell().Text("Payment No.").Bold();
                    h.Cell().Text("Customer").Bold();
                    h.Cell().Text("Method").Bold();
                    h.Cell().Text("Branch").Bold();
                    h.Cell().Text("Ref No.").Bold();
                    h.Cell().Text("Status").Bold();
                    h.Cell().AlignRight().Text("Amount").Bold();
                    h.Cell().AlignRight().Text("Running Total").Bold();
                });
                foreach (var row in report.Rows)
                {
                    table.Cell().Text(row.PaymentDate.ToString("yyyy-MM-dd"));
                    table.Cell().Text(row.PaymentNo);
                    table.Cell().Text(row.CustomerName);
                    table.Cell().Text(row.Method);
                    table.Cell().Text(row.BranchId);
                    table.Cell().Text(row.RefNo ?? "-");
                    table.Cell().Text(row.Status);
                    table.Cell().AlignRight().Text(row.Amount.ToString("N2"));
                    table.Cell().AlignRight().Text(row.RunningTotal.ToString("N2"));
                }

                table.Cell().ColumnSpan(7).Text("TOTAL RECEIVED (POSTED)").Bold();
                table.Cell().AlignRight().Text("");
                table.Cell().AlignRight().Text(report.TotalReceived.ToString("N2")).Bold();
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
