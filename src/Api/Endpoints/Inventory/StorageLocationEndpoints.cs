using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StorageLocations.Create;
using ZARI.Application.Features.Inventory.StorageLocations.Delete;
using ZARI.Application.Features.Inventory.StorageLocations.Get;
using ZARI.Application.Features.Inventory.StorageLocations.GetAll;
using ZARI.Application.Features.Inventory.StorageLocations.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class StorageLocationEndpoints
{
    public static void MapStorageLocationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/storage-locations")
            .WithTags("StorageLocations")
            .WithGroupName("Inventory")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllStorageLocations")
            .WithSummary("Get all storage locations");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetStorageLocationById")
            .WithSummary("Get a storage location by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateStorageLocationCommand>>()
            .WithName("CreateStorageLocation")
            .WithSummary("Create a new storage location");

        group.MapPut("/{id:guid}", Update)
            .AddEndpointFilter<ValidationFilter<UpdateStorageLocationCommand>>()
            .WithName("UpdateStorageLocation")
            .WithSummary("Update an existing storage location");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteStorageLocation")
            .WithSummary("Delete a storage location");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllStorageLocationsQuery, Result<List<StorageLocationResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllStorageLocationsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetStorageLocationQuery, Result<StorageLocationResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetStorageLocationQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateStorageLocationCommand command,
        ICommandHandler<CreateStorageLocationCommand, Result<StorageLocationResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetStorageLocationById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateStorageLocationRequest request,
        ICommandHandler<UpdateStorageLocationCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateStorageLocationCommand(id, request.WarehouseId, request.Zone, request.Aisle, request.Rack, request.BinCode);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteStorageLocationCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteStorageLocationCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateStorageLocationRequest(Guid WarehouseId, string? Zone, string? Aisle, string? Rack, string? BinCode);
