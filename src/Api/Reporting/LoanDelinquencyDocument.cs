using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZARI.Application.Features.Loan.Reports.LoanDelinquency;

namespace ZARI.Api.Reporting;

public sealed class LoanDelinquencyDocument(LoanDelinquencyReportResponse report) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(9));
            page.Header().Text("Loan Delinquency / Past-Due Report").FontSize(16).Bold();
            page.Content().Column(column =>
            {
                column.Item().PaddingTop(4).Text(
                    $"Total Outstanding: {report.TotalOutstanding:N2}   |   Current: {report.Current:N2}   |   1-30: {report.Days1To30:N2}   |   31-60: {report.Days31To60:N2}   |   61-90: {report.Days61To90:N2}   |   90+: {report.Days90Plus:N2}")
                    .FontSize(9).Bold();

                foreach (var group in report.Groups)
                {
                    column.Item().PaddingTop(10).Text($"{group.CustomerName}{(group.MemberNo is null ? "" : $" ({group.MemberNo})")} — {group.GroupTotal:N2}").Bold();
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(2);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Text("Loan Account").Bold();
                            h.Cell().Text("Installment #").Bold();
                            h.Cell().Text("Branch").Bold();
                            h.Cell().Text("Due Date").Bold();
                            h.Cell().Text("Bucket").Bold();
                            h.Cell().AlignRight().Text("Outstanding").Bold();
                        });
                        foreach (var row in group.Installments)
                        {
                            table.Cell().Text(row.LoanAcctNo);
                            table.Cell().Text(row.InstallmentNo.ToString());
                            table.Cell().Text(row.BranchId);
                            table.Cell().Text(row.DueDate.ToString("yyyy-MM-dd"));
                            table.Cell().Text($"{row.Bucket} ({row.DaysOverdue}d)");
                            table.Cell().AlignRight().Text(row.Outstanding.ToString("N2"));
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
