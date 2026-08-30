using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockLocationBalances.GetAll;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

/// <summary>
/// Read-only on purpose — see <see cref="StockLedgerEndpoints"/>'s doc comment for the full
/// rationale. <c>ReceiveIntoLocationCommand</c>/<c>MoveBetweenLocationsCommand</c> are internal
/// composition primitives called in-process from GoodsReceipt/GoodsIssue/etc.'s own Approve
/// handlers; the raw <c>POST /receive</c>/<c>/move</c> endpoints had no permission check and no
/// FE call site, so they were removed rather than permission-gated.
/// </summary>
public static class StockLocationBalanceEndpoints
{
    public static void MapStockLocationBalanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stock-location-balances")
            .WithTags("StockLocationBalances")
            .WithGroupName("Inventory")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllStockLocationBalances")
            .WithSummary("Get all bin-level stock balances");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllStockLocationBalancesQuery, Result<List<StockLocationBalanceResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllStockLocationBalancesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
