using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZARI.Application.Features.Purchasing.Reports.PurchaseBook;

namespace ZARI.Api.Reporting;

public sealed class PurchaseBookDocument(PurchaseBookReportResponse report) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(9));
            page.Header().Text("Purchase Book").FontSize(16).Bold();
            page.Content().Column(column =>
            {
                column.Item().PaddingTop(4).Text(
                    $"Gross: {report.TotalGross:N2}   |   VATable: {report.TotalVatableSales:N2}   |   Zero-Rated: {report.TotalZeroRated:N2}   |   Exempt: {report.TotalExempt:N2}   |   Input Tax: {report.TotalInputTax:N2}")
                    .FontSize(9).Bold();

                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("Date").Bold();
                        h.Cell().Text("Supplier").Bold();
                        h.Cell().Text("Supplier Inv. No.").Bold();
                        h.Cell().Text("Branch").Bold();
                        h.Cell().AlignRight().Text("Gross").Bold();
                        h.Cell().AlignRight().Text("VATable").Bold();
                        h.Cell().AlignRight().Text("Zero-Rated").Bold();
                        h.Cell().AlignRight().Text("Exempt").Bold();
                        h.Cell().AlignRight().Text("Input Tax").Bold();
                    });
                    foreach (var row in report.Rows)
                    {
                        table.Cell().Text(row.InvoiceDate.ToString("yyyy-MM-dd"));
                        table.Cell().Text($"{row.SupplierName}{(string.IsNullOrEmpty(row.SupplierTaxId) ? "" : $" (TIN: {row.SupplierTaxId})")}");
                        table.Cell().Text(row.SupplierInvoiceNo);
                        table.Cell().Text(row.BranchId);
                        table.Cell().AlignRight().Text(row.Gross.ToString("N2"));
                        table.Cell().AlignRight().Text(row.VatableSales.ToString("N2"));
                        table.Cell().AlignRight().Text(row.ZeroRated.ToString("N2"));
                        table.Cell().AlignRight().Text(row.Exempt.ToString("N2"));
                        table.Cell().AlignRight().Text(row.InputTax.ToString("N2"));
                    }
                    table.Footer(f =>
                    {
                        f.Cell().ColumnSpan(4).Text("Total").Bold();
                        f.Cell().AlignRight().Text(report.TotalGross.ToString("N2")).Bold();
                        f.Cell().AlignRight().Text(report.TotalVatableSales.ToString("N2")).Bold();
                        f.Cell().AlignRight().Text(report.TotalZeroRated.ToString("N2")).Bold();
                        f.Cell().AlignRight().Text(report.TotalExempt.ToString("N2")).Bold();
                        f.Cell().AlignRight().Text(report.TotalInputTax.ToString("N2")).Bold();
                    });
                });
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
