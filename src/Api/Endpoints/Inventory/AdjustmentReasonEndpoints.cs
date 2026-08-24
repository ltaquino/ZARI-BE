using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.AdjustmentReasons.Create;
using ZARI.Application.Features.Inventory.AdjustmentReasons.Delete;
using ZARI.Application.Features.Inventory.AdjustmentReasons.Get;
using ZARI.Application.Features.Inventory.AdjustmentReasons.GetAll;
using ZARI.Application.Features.Inventory.AdjustmentReasons.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class AdjustmentReasonEndpoints
{
    public static void MapAdjustmentReasonEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/adjustment-reasons")
            .WithTags("AdjustmentReasons")
            .WithGroupName("Inventory")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllAdjustmentReasons")
            .WithSummary("Get all adjustment reasons");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetAdjustmentReasonById")
            .WithSummary("Get an adjustment reason by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateAdjustmentReasonCommand>>()
            .WithName("CreateAdjustmentReason")
            .WithSummary("Create a new adjustment reason");

        group.MapPut("/{id:guid}", Update)
            .AddEndpointFilter<ValidationFilter<UpdateAdjustmentReasonCommand>>()
            .WithName("UpdateAdjustmentReason")
            .WithSummary("Update an existing adjustment reason");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteAdjustmentReason")
            .WithSummary("Delete an adjustment reason");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllAdjustmentReasonsQuery, Result<List<AdjustmentReasonResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllAdjustmentReasonsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetAdjustmentReasonQuery, Result<AdjustmentReasonResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAdjustmentReasonQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateAdjustmentReasonCommand command,
        ICommandHandler<CreateAdjustmentReasonCommand, Result<AdjustmentReasonResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetAdjustmentReasonById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateAdjustmentReasonRequest request,
        ICommandHandler<UpdateAdjustmentReasonCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateAdjustmentReasonCommand(id, request.Code, request.Description, request.GlAccountId, request.Status);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteAdjustmentReasonCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteAdjustmentReasonCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateAdjustmentReasonRequest(string Code, string? Description, string? GlAccountId, string Status);
