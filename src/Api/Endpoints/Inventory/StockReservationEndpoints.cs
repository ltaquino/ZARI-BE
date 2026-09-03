using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockReservations.Create;
using ZARI.Application.Features.Inventory.StockReservations.GetAll;
using ZARI.Application.Features.Inventory.StockReservations.GetAllPaged;
using ZARI.Application.Features.Inventory.StockReservations.Release;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class StockReservationEndpoints
{
    public static void MapStockReservationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stock-reservations")
            .WithTags("StockReservations")
            .WithGroupName("Inventory")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllStockReservations")
            .WithSummary("Get all stock reservations");

        group.MapGet("/paged", GetAllPaged)
            .WithName("GetAllStockReservationsPaged")
            .WithSummary("Get a page of stock reservations, optionally filtered by search text");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateStockReservationCommand>>()
            .WithName("CreateStockReservation")
            .WithSummary("Create a new stock reservation");

        group.MapPatch("/{id:guid}/release", Release)
            .WithName("ReleaseStockReservation")
            .WithSummary("Release an active stock reservation");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllStockReservationsQuery, Result<List<StockReservationResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllStockReservationsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetAllPaged(
        int? page,
        int? pageSize,
        string? search,
        IQueryHandler<GetAllStockReservationsPagedQuery, Result<PagedResult<StockReservationResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllStockReservationsPagedQuery(page ?? 1, pageSize ?? 20, search), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateStockReservationCommand command,
        ICommandHandler<CreateStockReservationCommand, Result<StockReservationResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Release(
        Guid id,
        ReleaseStockReservationRequest? request,
        ICommandHandler<ReleaseStockReservationCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ReleaseStockReservationCommand(id, request?.ReleasedBy), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record ReleaseStockReservationRequest(string? ReleasedBy);
