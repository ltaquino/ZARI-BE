using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZARI.Application.Features.Inventory.Reports.InventoryValuation;

namespace ZARI.Api.Reporting;

public sealed class InventoryValuationDocument(InventoryValuationReportResponse report) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(9));
            page.Header().Text("Inventory Valuation").FontSize(16).Bold();
            page.Content().PaddingTop(10).Column(column =>
            {
                column.Spacing(12);

                foreach (var branch in report.Branches)
                {
                    column.Item().Column(branchBlock =>
                    {
                        branchBlock.Item().Text($"Branch: {branch.BranchId}").FontSize(11).Bold();
                        branchBlock.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(5); // Category
                                c.RelativeColumn(2); // Qty on Hand
                                c.RelativeColumn(3); // Total Value
                            });
                            table.Header(h =>
                            {
                                h.Cell().Text("Category").Bold();
                                h.Cell().AlignRight().Text("Qty on Hand").Bold();
                                h.Cell().AlignRight().Text("Total Value").Bold();
                            });
                            foreach (var category in branch.Categories)
                            {
                                table.Cell().Text(category.CategoryName);
                                table.Cell().AlignRight().Text(category.QtyOnHand.ToString("N2"));
                                table.Cell().AlignRight().Text(category.TotalValue.ToString("N2"));
                            }

                            table.Cell().Text("Branch Total").Bold();
                            table.Cell().AlignRight().Text("");
                            table.Cell().AlignRight().Text(branch.BranchTotalValue.ToString("N2")).Bold();
                        });
                    });
                }

                column.Item().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem().Text("GRAND TOTAL").Bold();
                    row.RelativeItem().AlignRight().Text($"Qty: {report.GrandTotalQty:N2}").Bold();
                    row.RelativeItem().AlignRight().Text($"Value: {report.GrandTotalValue:N2}").Bold();
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
