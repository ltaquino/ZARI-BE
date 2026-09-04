using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZARI.Application.Features.Sales.Reports.SalesBook;

namespace ZARI.Api.Reporting;

public sealed class SalesBookDocument(SalesBookReportResponse report) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(9));
            page.Header().Text("Sales Book").FontSize(16).Bold();
            page.Content().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2); // Date
                    c.RelativeColumn(3); // Invoice No.
                    c.RelativeColumn(2); // OR/SI No.
                    c.RelativeColumn(4); // Customer
                    c.RelativeColumn(2); // Branch
                    c.RelativeColumn(2); // Gross
                    c.RelativeColumn(2); // VATable
                    c.RelativeColumn(2); // Zero-Rated
                    c.RelativeColumn(2); // Exempt
                    c.RelativeColumn(2); // VAT
                });
                table.Header(h =>
                {
                    h.Cell().Text("Date").Bold();
                    h.Cell().Text("Invoice No.").Bold();
                    h.Cell().Text("OR/SI No.").Bold();
                    h.Cell().Text("Customer").Bold();
                    h.Cell().Text("Branch").Bold();
                    h.Cell().AlignRight().Text("Gross").Bold();
                    h.Cell().AlignRight().Text("VATable").Bold();
                    h.Cell().AlignRight().Text("Zero-Rated").Bold();
                    h.Cell().AlignRight().Text("Exempt").Bold();
                    h.Cell().AlignRight().Text("VAT").Bold();
                });
                foreach (var row in report.Rows)
                {
                    table.Cell().Text(row.InvoiceDate.ToString("yyyy-MM-dd"));
                    table.Cell().Text(row.InvoiceNo);
                    table.Cell().Text(row.BirOrSeriesNumber ?? "-");
                    table.Cell().Text(row.CustomerName);
                    table.Cell().Text(row.BranchId);
                    table.Cell().AlignRight().Text(row.Gross.ToString("N2"));
                    table.Cell().AlignRight().Text(row.VatableSales.ToString("N2"));
                    table.Cell().AlignRight().Text(row.ZeroRated.ToString("N2"));
                    table.Cell().AlignRight().Text(row.Exempt.ToString("N2"));
                    table.Cell().AlignRight().Text(row.VatAmount.ToString("N2"));
                }

                table.Cell().ColumnSpan(5).Text("TOTAL").Bold();
                table.Cell().AlignRight().Text(report.TotalGross.ToString("N2")).Bold();
                table.Cell().AlignRight().Text(report.TotalVatableSales.ToString("N2")).Bold();
                table.Cell().AlignRight().Text(report.TotalZeroRated.ToString("N2")).Bold();
                table.Cell().AlignRight().Text(report.TotalExempt.ToString("N2")).Bold();
                table.Cell().AlignRight().Text(report.TotalVatAmount.ToString("N2")).Bold();
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
