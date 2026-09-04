using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZARI.Application.Features.Reporting.ReportTemplates.Run;

namespace ZARI.Api.Reporting;

/// <summary>
/// PDF renderer for any Report Designer template's Run result. Unlike the other Reporting/*
/// documents (fixed column sets known at compile time), this one takes a dynamic column list —
/// the table's ColumnsDefinition, header row, and each data row's cells are all built via foreach
/// loops over the columns the template chose, rather than one hardcoded RelativeColumn call per
/// column.
/// </summary>
public sealed class GenericTableReportDocument(RunReportTemplateResponse report) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        var pageSize = report.PaperSize switch
        {
            "Letter" => PageSizes.Letter,
            "Legal" => PageSizes.Legal,
            _ => PageSizes.A4
        };

        if (string.Equals(report.Orientation, "Landscape", StringComparison.OrdinalIgnoreCase))
            pageSize = pageSize.Landscape();

        container.Page(page =>
        {
            page.Size(pageSize);
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(9));

            page.Header().Column(headerColumn =>
            {
                headerColumn.Item().Text(report.TemplateName).FontSize(16).Bold();
                if (!string.IsNullOrWhiteSpace(report.HeaderText))
                    headerColumn.Item().PaddingTop(4).Text(report.HeaderText).FontSize(9);
            });

            page.Content().Column(column =>
            {
                if (report.Truncated)
                    column.Item().PaddingBottom(6).Text("Showing first 20,000 rows — results truncated.").FontSize(8).Italic();

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        foreach (var _ in report.Columns)
                            c.RelativeColumn();
                    });

                    table.Header(h =>
                    {
                        foreach (var col in report.Columns)
                        {
                            if (IsNumeric(col.Type))
                                h.Cell().AlignRight().Text(col.Label).Bold();
                            else
                                h.Cell().Text(col.Label).Bold();
                        }
                    });

                    foreach (var row in report.Rows)
                    {
                        foreach (var col in report.Columns)
                        {
                            row.TryGetValue(col.FieldKey, out var value);
                            var text = FormatValue(value, col.Type);

                            if (IsNumeric(col.Type))
                                table.Cell().AlignRight().Text(text);
                            else
                                table.Cell().Text(text);
                        }
                    }

                    if (report.ShowColumnTotals)
                    {
                        foreach (var col in report.Columns)
                        {
                            if (IsNumeric(col.Type))
                            {
                                var total = report.Rows.Sum(r => ToDecimalOrZero(r.GetValueOrDefault(col.FieldKey)));
                                table.Cell().AlignRight().Text(total.ToString("N2")).Bold();
                            }
                            else
                            {
                                table.Cell().Text(string.Empty);
                            }
                        }
                    }
                });
            });

            page.Footer().Column(footerColumn =>
            {
                if (!string.IsNullOrWhiteSpace(report.FooterText))
                    footerColumn.Item().AlignCenter().Text(report.FooterText).FontSize(8);

                footerColumn.Item().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });
    }

    private static bool IsNumeric(string type) => type is "Currency" or "Number";

    private static string FormatValue(object? value, string type)
    {
        if (value is null) return "-";

        return type switch
        {
            "Currency" => ToDecimalOrZero(value).ToString("N2"),
            "Number" => ToDecimalOrZero(value).ToString("N2"),
            "Date" => ToDateTimeOrNull(value) is { } dt ? dt.ToString("yyyy-MM-dd") : "-",
            "Boolean" => ToBoolOrNull(value) is { } b ? (b ? "Yes" : "No") : "-",
            _ => value.ToString() ?? "-"
        };
    }

    private static decimal ToDecimalOrZero(object? value)
    {
        if (value is null) return 0m;
        try { return Convert.ToDecimal(value); } catch { return 0m; }
    }

    private static DateTime? ToDateTimeOrNull(object? value)
    {
        if (value is null) return null;
        try { return Convert.ToDateTime(value); } catch { return null; }
    }

    private static bool? ToBoolOrNull(object? value)
    {
        if (value is null) return null;
        try { return Convert.ToBoolean(value); } catch { return null; }
    }
}
