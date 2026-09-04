using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZARI.Application.Features.Inventory.StockLedgers.GetInventoryAsOf;

namespace ZARI.Api.Reporting;

public sealed class AnnualInventoryListDocument(List<InventoryAsOfLineResponse> lines, DateTimeOffset asOfDate) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(9));
            page.Header().Column(headerColumn =>
            {
                headerColumn.Item().Text("Annual Inventory List").FontSize(16).Bold();
                headerColumn.Item().Text($"As of {asOfDate:yyyy-MM-dd}").FontSize(9);
            });
            page.Content().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2); // Item Code
                    c.RelativeColumn(4); // Item Name
                    c.RelativeColumn(2); // Branch
                    c.RelativeColumn(3); // Warehouse
                    c.RelativeColumn(2); // Batch
                    c.RelativeColumn(2); // Qty on Hand
                    c.RelativeColumn(2); // Avg Unit Cost
                    c.RelativeColumn(2); // Total Value
                });
                table.Header(h =>
                {
                    h.Cell().Text("Item Code").Bold();
                    h.Cell().Text("Item Name").Bold();
                    h.Cell().Text("Branch").Bold();
                    h.Cell().Text("Warehouse").Bold();
                    h.Cell().Text("Batch").Bold();
                    h.Cell().AlignRight().Text("Qty on Hand").Bold();
                    h.Cell().AlignRight().Text("Avg Unit Cost").Bold();
                    h.Cell().AlignRight().Text("Total Value").Bold();
                });
                foreach (var line in lines)
                {
                    table.Cell().Text(line.ItemCode ?? "-");
                    table.Cell().Text(line.ItemName ?? "-");
                    table.Cell().Text(line.BranchId);
                    table.Cell().Text(line.WarehouseName);
                    table.Cell().Text(line.BatchNo ?? "-");
                    table.Cell().AlignRight().Text(line.QtyOnHand.ToString("N2"));
                    table.Cell().AlignRight().Text(line.AvgUnitCost.ToString("N4"));
                    table.Cell().AlignRight().Text(line.TotalValue.ToString("N2"));
                }

                table.Cell().ColumnSpan(5).Text("TOTAL").Bold();
                table.Cell().AlignRight().Text(lines.Sum(l => l.QtyOnHand).ToString("N2")).Bold();
                table.Cell().AlignRight().Text("");
                table.Cell().AlignRight().Text(lines.Sum(l => l.TotalValue).ToString("N2")).Bold();
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
