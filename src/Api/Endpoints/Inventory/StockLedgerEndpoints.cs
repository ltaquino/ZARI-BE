using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockLedgers.GetBalances;
using ZARI.Application.Features.Inventory.StockLedgers.GetLedgerEntries;
using ZARI.Application.Features.Inventory.StockLedgers.Issue;
using ZARI.Application.Features.Inventory.StockLedgers.Receive;
using ZARI.Application.Features.Inventory.StockLedgers.Reverse;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

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

        group.MapPost("/receive", Receive)
            .AddEndpointFilter<ValidationFilter<ReceiveStockCommand>>()
            .WithName("ReceiveStock")
            .WithSummary("Post a stock-in movement (e.g. a Goods Receipt line)");

        group.MapPost("/issue", Issue)
            .AddEndpointFilter<ValidationFilter<IssueStockLinesCommand>>()
            .WithName("IssueStockLines")
            .WithSummary("Post a batch of stock-out movements (e.g. a Goods Issue's lines)");

        group.MapPost("/reverse", Reverse)
            .AddEndpointFilter<ValidationFilter<ReverseStockMovementsCommand>>()
            .WithName("ReverseStockMovements")
            .WithSummary("Reverse every previously posted movement for a set of (referenceTable, referenceId) lines");
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

    private static async Task<IResult> Receive(
        ReceiveStockCommand command,
        ICommandHandler<ReceiveStockCommand, Result<ReceiveStockResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Issue(
        IssueStockLinesCommand command,
        ICommandHandler<IssueStockLinesCommand, Result<IssueStockLinesResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Reverse(
        ReverseStockMovementsCommand command,
        ICommandHandler<ReverseStockMovementsCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
