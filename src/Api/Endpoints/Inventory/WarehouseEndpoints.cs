using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Warehouses.Create;
using ZARI.Application.Features.Inventory.Warehouses.Delete;
using ZARI.Application.Features.Inventory.Warehouses.Get;
using ZARI.Application.Features.Inventory.Warehouses.GetAll;
using ZARI.Application.Features.Inventory.Warehouses.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class WarehouseEndpoints
{
    public static void MapWarehouseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/warehouses")
            .WithTags("Warehouses")
            .WithGroupName("Inventory")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllWarehouses")
            .WithSummary("Get all warehouses");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetWarehouseById")
            .WithSummary("Get a warehouse by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateWarehouseCommand>>()
            .WithName("CreateWarehouse")
            .WithSummary("Create a new warehouse");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateWarehouse")
            .WithSummary("Update an existing warehouse");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteWarehouse")
            .WithSummary("Delete a warehouse");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllWarehousesQuery, Result<List<WarehouseResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllWarehousesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetWarehouseQuery, Result<WarehouseResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetWarehouseQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateWarehouseCommand command,
        ICommandHandler<CreateWarehouseCommand, Result<WarehouseResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetWarehouseById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateWarehouseRequest request,
        IValidator<UpdateWarehouseCommand> validator,
        ICommandHandler<UpdateWarehouseCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateWarehouseCommand(id, request.BranchId, request.Code, request.Name, request.WarehouseType, request.Status);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteWarehouseCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteWarehouseCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateWarehouseRequest(string BranchId, string Code, string Name, string WarehouseType, string Status);
