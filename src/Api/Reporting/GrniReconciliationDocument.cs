using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZARI.Application.Features.Purchasing.Reports.GrniReconciliation;

namespace ZARI.Api.Reporting;

public sealed class GrniReconciliationDocument(GrniReconciliationReportResponse report) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(9));
            page.Header().Text("GRNI Reconciliation Report").FontSize(16).Bold();
            page.Content().Column(column =>
            {
                column.Item().PaddingTop(4).Text(
                    $"Total Received: {report.TotalReceived:N2}   |   Cleared: {report.TotalCleared:N2}   |   Outstanding (documents): {report.TotalOutstanding:N2}   |   Live GL \"2100\" Balance: {report.LiveGrniBalance:N2}   |   Variance: {report.Variance:N2}   |   {(report.IsReconciled ? "RECONCILED" : "NOT RECONCILED")}")
                    .FontSize(9).Bold();

                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2);
                        c.RelativeColumn(1);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(3);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("GRPO No.").Bold();
                        h.Cell().Text("Branch").Bold();
                        h.Cell().Text("Supplier").Bold();
                        h.Cell().Text("Receipt Date").Bold();
                        h.Cell().AlignRight().Text("Value").Bold();
                        h.Cell().AlignRight().Text("Cleared").Bold();
                        h.Cell().AlignRight().Text("Outstanding").Bold();
                        h.Cell().Text("Cleared By").Bold();
                    });
                    foreach (var row in report.Rows)
                    {
                        table.Cell().Text(row.GrpoNo);
                        table.Cell().Text(row.BranchId);
                        table.Cell().Text(row.SupplierName);
                        table.Cell().Text(row.ReceiptDate.ToString("yyyy-MM-dd"));
                        table.Cell().AlignRight().Text(row.Value.ToString("N2"));
                        table.Cell().AlignRight().Text(row.ClearedValue.ToString("N2"));
                        table.Cell().AlignRight().Text(row.Outstanding.ToString("N2"));
                        table.Cell().Text(row.ClearedByDocumentNos.Count == 0 ? "Not yet" : string.Join(", ", row.ClearedByDocumentNos));
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
