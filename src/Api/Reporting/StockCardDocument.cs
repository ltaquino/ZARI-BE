using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZARI.Application.Features.Inventory.StockLedgers.GetLedgerEntries;

namespace ZARI.Api.Reporting;

public sealed class StockCardDocument(List<StockLedgerEntryResponse> entries) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        var header = entries.FirstOrDefault();

        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(9));
            page.Header().Column(headerColumn =>
            {
                headerColumn.Item().Text("Stock Card").FontSize(16).Bold();
                if (header is not null)
                    headerColumn.Item().Text($"{header.ItemCode} - {header.ItemName} ({header.UomCode})  |  Warehouse: {header.WarehouseId}  |  Batch: {header.BatchNo ?? "-"}").FontSize(9);
            });
            page.Content().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2); // Date
                    c.RelativeColumn(3); // Transaction Type
                    c.RelativeColumn(3); // Reference
                    c.RelativeColumn(2); // Qty In
                    c.RelativeColumn(2); // Qty Out
                    c.RelativeColumn(2); // Unit Cost
                    c.RelativeColumn(2); // Balance Qty
                    c.RelativeColumn(2); // Balance Value
                });
                table.Header(h =>
                {
                    h.Cell().Text("Date").Bold();
                    h.Cell().Text("Transaction").Bold();
                    h.Cell().Text("Reference").Bold();
                    h.Cell().AlignRight().Text("Qty In").Bold();
                    h.Cell().AlignRight().Text("Qty Out").Bold();
                    h.Cell().AlignRight().Text("Unit Cost").Bold();
                    h.Cell().AlignRight().Text("Bal. Qty").Bold();
                    h.Cell().AlignRight().Text("Bal. Value").Bold();
                });
                foreach (var entry in entries)
                {
                    table.Cell().Text(entry.TransactionDate.ToString("yyyy-MM-dd"));
                    table.Cell().Text(entry.TransactionType + (entry.IsReversal ? " (REV)" : ""));
                    table.Cell().Text($"{entry.ReferenceTable} {entry.ReferenceId}");
                    table.Cell().AlignRight().Text(entry.QtyIn.ToString("N2"));
                    table.Cell().AlignRight().Text(entry.QtyOut.ToString("N2"));
                    table.Cell().AlignRight().Text(entry.UnitCost.ToString("N4"));
                    table.Cell().AlignRight().Text(entry.RunningBalanceQty.ToString("N2"));
                    table.Cell().AlignRight().Text(entry.RunningBalanceValue.ToString("N2"));
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
