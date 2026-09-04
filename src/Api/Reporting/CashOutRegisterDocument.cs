using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZARI.Application.Features.Purchasing.Reports.CashOutRegister;

namespace ZARI.Api.Reporting;

public sealed class CashOutRegisterDocument(CashOutRegisterReportResponse report) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(9));
            page.Header().Text("Cash-Out Register (Cash Disbursements Book)").FontSize(16).Bold();
            page.Content().Column(column =>
            {
                column.Item().PaddingTop(4).Text($"Total Paid Out: {report.TotalPaidOut:N2}").FontSize(9).Bold();

                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("Date").Bold();
                        h.Cell().Text("Payment No.").Bold();
                        h.Cell().Text("Supplier").Bold();
                        h.Cell().Text("Paid From").Bold();
                        h.Cell().Text("Ref No.").Bold();
                        h.Cell().Text("Branch").Bold();
                        h.Cell().Text("Status").Bold();
                        h.Cell().AlignRight().Text("Amount").Bold();
                        h.Cell().AlignRight().Text("Running Total").Bold();
                    });
                    foreach (var row in report.Rows)
                    {
                        table.Cell().Text(row.PaymentDate.ToString("yyyy-MM-dd"));
                        table.Cell().Text(row.PaymentNo);
                        table.Cell().Text(row.SupplierName);
                        table.Cell().Text(row.BankAccountName);
                        table.Cell().Text(row.RefNo ?? "-");
                        table.Cell().Text(row.BranchId);
                        table.Cell().Text(row.Status);
                        table.Cell().AlignRight().Text(row.Amount.ToString("N2"));
                        table.Cell().AlignRight().Text(row.RunningTotal.ToString("N2"));
                    }
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
