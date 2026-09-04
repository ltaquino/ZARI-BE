using QuestPDF.Fluent;
using ZARI.Api.Extensions;
using ZARI.Api.Reporting;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockLedgers.GetBalances;
using ZARI.Application.Features.Inventory.StockLedgers.GetInventoryAsOf;
using ZARI.Application.Features.Inventory.StockLedgers.GetLedgerEntries;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

/// <summary>
/// Read-only on purpose. Receiving/issuing/reversing stock is never a direct user action — every
/// document (GoodsReceipt, GoodsIssue, StockAdjustment, StockOpname, etc.) posts stock through its
/// own Approve handler (which already enforces that module's own branch/permission checks) via
/// in-process <c>ICommandHandler&lt;ReceiveStockCommand,...&gt;</c>/
/// <c>ICommandHandler&lt;IssueStockLinesCommand,...&gt;</c>/
/// <c>ICommandHandler&lt;ReverseStockMovementsCommand,...&gt;</c> injection, never over HTTP. These
/// three commands used to also be mapped as raw <c>POST /api/stock-ledger/receive|issue|reverse</c>
/// endpoints with no permission check of their own and no GL posting — since nothing legitimate
/// ever called them (confirmed: no FE call site exists), that was a live authorization bypass
/// letting any authenticated user silently mutate stock balances/cost layers for any item,
/// warehouse, or branch with a fabricated reference and no corresponding GL journal. Removed
/// rather than permission-gated, matching the same fix applied to GlJournalEndpoints.
/// </summary>
public static class StockLedgerEndpoints
{
    public static void MapStockLedgerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stock-ledger")
            .WithTags("StockLedger")
            .WithGroupName("Inventory")
            .RequireAuthorization();

        group.MapGet("/balances", GetBalances)
            .WithName("ListStockBalances")
            .WithSummary("List the current on-hand balance for every (item, warehouse, batch)");

        group.MapGet("/entries", GetLedgerEntries)
            .WithName("ListStockLedgerEntries")
            .WithSummary("List the movement history for one (item, warehouse, batch)");

        group.MapGet("/entries/pdf", GetLedgerEntriesPdf)
            .WithName("ListStockLedgerEntriesPdf")
            .WithSummary("The Stock Card for one (item, warehouse, batch), as a PDF");

        group.MapGet("/as-of", GetInventoryAsOf)
            .WithName("GetInventoryAsOf")
            .WithSummary("Reconstruct true point-in-time ending inventory balances as of any date — the BIR Annual Inventory List");

        group.MapGet("/as-of/pdf", GetInventoryAsOfPdf)
            .WithName("GetInventoryAsOfPdf")
            .WithSummary("The BIR Annual Inventory List, as a PDF");
    }

    private static async Task<IResult> GetBalances(
        IQueryHandler<ListStockBalancesQuery, Result<List<StockBalanceResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListStockBalancesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetLedgerEntries(
        Guid itemId,
        Guid warehouseId,
        string? batchNo,
        IQueryHandler<ListStockLedgerEntriesQuery, Result<List<StockLedgerEntryResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListStockLedgerEntriesQuery(itemId, warehouseId, batchNo), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetInventoryAsOf(
        DateTimeOffset date,
        string? branchId,
        bool? includeZero,
        IQueryHandler<GetInventoryAsOfQuery, Result<List<InventoryAsOfLineResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetInventoryAsOfQuery(date, branchId, includeZero ?? false), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetLedgerEntriesPdf(
        Guid itemId,
        Guid warehouseId,
        string? batchNo,
        IQueryHandler<ListStockLedgerEntriesQuery, Result<List<StockLedgerEntryResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListStockLedgerEntriesQuery(itemId, warehouseId, batchNo), cancellationToken);
        if (!result.IsSuccess) return result.ToProblemDetails();

        var bytes = new StockCardDocument(result.Value!).GeneratePdf();
        return Results.File(bytes, "application/pdf", "stock-card.pdf");
    }

    private static async Task<IResult> GetInventoryAsOfPdf(
        DateTimeOffset date,
        string? branchId,
        bool? includeZero,
        IQueryHandler<GetInventoryAsOfQuery, Result<List<InventoryAsOfLineResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetInventoryAsOfQuery(date, branchId, includeZero ?? false), cancellationToken);
        if (!result.IsSuccess) return result.ToProblemDetails();

        var bytes = new AnnualInventoryListDocument(result.Value!, date).GeneratePdf();
        return Results.File(bytes, "application/pdf", "annual-inventory-list.pdf");
    }
}
