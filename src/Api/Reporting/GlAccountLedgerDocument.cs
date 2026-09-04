using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZARI.Application.Features.Accounting.Reports.GlAccountLedger;

namespace ZARI.Api.Reporting;

public sealed class GlAccountLedgerDocument(GlAccountLedgerReportResponse report) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(9));

            page.Header().Column(col =>
            {
                col.Item().Text($"GL Account Ledger — {report.AccountCode} {report.AccountName}").FontSize(16).Bold();
                col.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Text($"Opening: {report.Opening:N2}");
                    row.RelativeItem().Text($"Period Debit: {report.PeriodDebit:N2}");
                    row.RelativeItem().Text($"Period Credit: {report.PeriodCredit:N2}");
                    row.RelativeItem().Text($"Closing: {report.Closing:N2}");
                });
            });

            page.Content().PaddingTop(8).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Text("Date").Bold();
                    header.Cell().Text("Journal No.").Bold();
                    header.Cell().Text("Branch").Bold();
                    header.Cell().Text("Memo").Bold();
                    header.Cell().AlignRight().Text("Debit").Bold();
                    header.Cell().AlignRight().Text("Credit").Bold();
                    header.Cell().AlignRight().Text("Running Balance").Bold();
                });

                table.Cell().ColumnSpan(6).Text("Opening balance").Italic();
                table.Cell().AlignRight().Text(report.Opening.ToString("N2")).Bold();

                foreach (var line in report.Lines)
                {
                    table.Cell().Text(line.JournalDate.ToString("yyyy-MM-dd"));
                    table.Cell().Text(line.JournalNo);
                    table.Cell().Text(line.BranchId);
                    table.Cell().Text(line.Memo ?? "-");
                    table.Cell().AlignRight().Text(line.Debit.ToString("N2"));
                    table.Cell().AlignRight().Text(line.Credit.ToString("N2"));
                    table.Cell().AlignRight().Text(line.RunningBalance.ToString("N2"));
                }

                table.Cell().ColumnSpan(6).Text("Closing balance").Bold();
                table.Cell().AlignRight().Text(report.Closing.ToString("N2")).Bold();
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
