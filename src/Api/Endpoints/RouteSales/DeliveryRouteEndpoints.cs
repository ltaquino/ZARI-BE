using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.DeliveryRoutes.Create;
using ZARI.Application.Features.RouteSales.DeliveryRoutes.Delete;
using ZARI.Application.Features.RouteSales.DeliveryRoutes.Get;
using ZARI.Application.Features.RouteSales.DeliveryRoutes.GetAll;
using ZARI.Application.Features.RouteSales.DeliveryRoutes.Shared;
using ZARI.Application.Features.RouteSales.DeliveryRoutes.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class DeliveryRouteEndpoints
{
    public static void MapDeliveryRouteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/delivery-routes")
            .WithTags("DeliveryRoutes")
            .WithGroupName("RouteSales")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllDeliveryRoutes")
            .WithSummary("Get all delivery routes, optionally filtered by branch");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetDeliveryRouteById")
            .WithSummary("Get a delivery route with its stops");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateDeliveryRouteCommand>>()
            .WithName("CreateDeliveryRoute")
            .WithSummary("Create a delivery route; stop order = visiting order");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateDeliveryRoute")
            .WithSummary("Update a delivery route, replacing its stops");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteDeliveryRoute")
            .WithSummary("Delete a delivery route");
    }

    private static async Task<IResult> GetAll(
        string? branchId,
        IQueryHandler<GetAllDeliveryRoutesQuery, Result<List<DeliveryRouteResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllDeliveryRoutesQuery(branchId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetDeliveryRouteQuery, Result<DeliveryRouteResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetDeliveryRouteQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateDeliveryRouteCommand command,
        ICommandHandler<CreateDeliveryRouteCommand, Result<DeliveryRouteResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetDeliveryRouteById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateDeliveryRouteRequest request,
        IValidator<UpdateDeliveryRouteCommand> validator,
        ICommandHandler<UpdateDeliveryRouteCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateDeliveryRouteCommand(id, request.Code, request.Name, request.Description, request.DefaultVehicleId,
            request.Status, request.Stops);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteDeliveryRouteCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteDeliveryRouteCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateDeliveryRouteRequest(
    string Code,
    string Name,
    string? Description,
    Guid? DefaultVehicleId,
    string Status,
    List<DeliveryRouteStopInput> Stops);
