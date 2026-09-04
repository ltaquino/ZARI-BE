using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZARI.Application.Features.Accounting.Reports.GeneralJournal;

namespace ZARI.Api.Reporting;

public sealed class GeneralJournalDocument(GeneralJournalReportResponse report) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(9));

            page.Header().Text("General Journal").FontSize(16).Bold();

            page.Content().Column(column =>
            {
                column.Spacing(6);

                foreach (var journal in report.Journals)
                {
                    column.Item().Column(entryColumn =>
                    {
                        entryColumn.Item().Background(Colors.Grey.Lighten3).Padding(4).Row(row =>
                        {
                            row.RelativeItem().Text($"{journal.JournalDate:yyyy-MM-dd} — {journal.JournalNo}").Bold();
                            row.RelativeItem().AlignRight().Text($"{journal.Description ?? ""} ({journal.BranchId}){(journal.Status == "REVERSED" ? " [REVERSED]" : "")}");
                        });

                        entryColumn.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(5);
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Account").Bold();
                                header.Cell().Text("Memo").Bold();
                                header.Cell().AlignRight().Text("Debit").Bold();
                                header.Cell().AlignRight().Text("Credit").Bold();
                            });

                            foreach (var line in journal.Lines)
                            {
                                table.Cell().PaddingLeft(8).Text(line.AccountName);
                                table.Cell().Text(line.Memo ?? "-");
                                table.Cell().AlignRight().Text(line.Debit.ToString("N2"));
                                table.Cell().AlignRight().Text(line.Credit.ToString("N2"));
                            }
                        });
                    });
                }

                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem(9).AlignRight().Text("TOTAL").Bold();
                    row.RelativeItem(2).AlignRight().Text(report.TotalDebit.ToString("N2")).Bold();
                    row.RelativeItem(2).AlignRight().Text(report.TotalCredit.ToString("N2")).Bold();
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
