using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockLocationBalances.GetAll;
using ZARI.Application.Features.Inventory.StockLocationBalances.Move;
using ZARI.Application.Features.Inventory.StockLocationBalances.Receive;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

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

        group.MapPost("/receive", Receive)
            .AddEndpointFilter<ValidationFilter<ReceiveIntoLocationCommand>>()
            .WithName("ReceiveIntoLocation")
            .WithSummary("Assign freshly-received qty to a bin");

        group.MapPost("/move", Move)
            .AddEndpointFilter<ValidationFilter<MoveBetweenLocationsCommand>>()
            .WithName("MoveBetweenLocations")
            .WithSummary("Move qty from one bin to another within the same warehouse");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllStockLocationBalancesQuery, Result<List<StockLocationBalanceResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllStockLocationBalancesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Receive(
        ReceiveIntoLocationCommand command,
        ICommandHandler<ReceiveIntoLocationCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Move(
        MoveBetweenLocationsCommand command,
        ICommandHandler<MoveBetweenLocationsCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
