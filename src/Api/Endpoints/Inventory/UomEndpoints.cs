using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Uoms.Create;
using ZARI.Application.Features.Inventory.Uoms.Delete;
using ZARI.Application.Features.Inventory.Uoms.Get;
using ZARI.Application.Features.Inventory.Uoms.GetAll;
using ZARI.Application.Features.Inventory.Uoms.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class UomEndpoints
{
    public static void MapUomEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/uoms")
            .WithTags("Uoms")
            .WithGroupName("Inventory")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllUoms")
            .WithSummary("Get all units of measure");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetUomById")
            .WithSummary("Get a unit of measure by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateUomCommand>>()
            .WithName("CreateUom")
            .WithSummary("Create a new unit of measure");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateUom")
            .WithSummary("Update an existing unit of measure");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteUom")
            .WithSummary("Delete a unit of measure");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllUomsQuery, Result<List<UomResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllUomsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetUomQuery, Result<UomResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetUomQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateUomCommand command,
        ICommandHandler<CreateUomCommand, Result<UomResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetUomById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateUomRequest request,
        IValidator<UpdateUomCommand> validator,
        ICommandHandler<UpdateUomCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUomCommand(id, request.Code, request.Name);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteUomCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteUomCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateUomRequest(string Code, string Name);
