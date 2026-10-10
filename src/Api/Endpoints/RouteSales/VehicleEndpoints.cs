using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.Vehicles.Create;
using ZARI.Application.Features.RouteSales.Vehicles.Delete;
using ZARI.Application.Features.RouteSales.Vehicles.Get;
using ZARI.Application.Features.RouteSales.Vehicles.GetAll;
using ZARI.Application.Features.RouteSales.Vehicles.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class VehicleEndpoints
{
    public static void MapVehicleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/vehicles")
            .WithTags("Vehicles")
            .WithGroupName("RouteSales")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllVehicles")
            .WithSummary("Get all delivery vehicles, optionally filtered by branch");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetVehicleById")
            .WithSummary("Get a delivery vehicle by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateVehicleCommand>>()
            .WithName("CreateVehicle")
            .WithSummary("Create a delivery vehicle with its own warehouse and own-series POS terminal");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateVehicle")
            .WithSummary("Update a delivery vehicle");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteVehicle")
            .WithSummary("Delete a delivery vehicle that has no stock or invoice history");
    }

    private static async Task<IResult> GetAll(
        string? branchId,
        IQueryHandler<GetAllVehiclesQuery, Result<List<VehicleResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllVehiclesQuery(branchId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetVehicleQuery, Result<VehicleResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetVehicleQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateVehicleCommand command,
        ICommandHandler<CreateVehicleCommand, Result<VehicleResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetVehicleById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateVehicleRequest request,
        IValidator<UpdateVehicleCommand> validator,
        ICommandHandler<UpdateVehicleCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateVehicleCommand(id, request.PlateNo, request.Description, request.Status, request.OrPrefix,
            request.OrNextNumber, request.OrPaddingLength, request.MachineIdentificationNumber, request.MachineSerialNumber,
            request.PosPermitNumber, request.PosPermitDateIssued);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteVehicleCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteVehicleCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateVehicleRequest(
    string PlateNo,
    string? Description,
    string Status,
    string OrPrefix,
    int? OrNextNumber = null,
    int? OrPaddingLength = null,
    string? MachineIdentificationNumber = null,
    string? MachineSerialNumber = null,
    string? PosPermitNumber = null,
    DateTime? PosPermitDateIssued = null);
