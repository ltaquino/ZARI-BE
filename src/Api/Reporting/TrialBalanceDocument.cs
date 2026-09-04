using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZARI.Application.Features.Accounting.Reports.TrialBalance;

namespace ZARI.Api.Reporting;

public sealed class TrialBalanceDocument(TrialBalanceReportResponse report) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(9));

            page.Header().Text("Trial Balance").FontSize(16).Bold();

            page.Content().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Text("Code").Bold();
                    header.Cell().Text("Account").Bold();
                    header.Cell().Text("Type").Bold();
                    header.Cell().AlignRight().Text("Debit").Bold();
                    header.Cell().AlignRight().Text("Credit").Bold();
                });

                foreach (var row in report.Rows)
                {
                    table.Cell().Text(row.Code);
                    table.Cell().Text(row.Name);
                    table.Cell().Text(row.AccountType);
                    table.Cell().AlignRight().Text(row.DebitBalance.ToString("N2"));
                    table.Cell().AlignRight().Text(row.CreditBalance.ToString("N2"));
                }

                table.Cell().ColumnSpan(3).Text("TOTAL").Bold();
                table.Cell().AlignRight().Text(report.TotalDebit.ToString("N2")).Bold();
                table.Cell().AlignRight().Text(report.TotalCredit.ToString("N2")).Bold();
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
