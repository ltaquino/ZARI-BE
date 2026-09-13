using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.Positions.Create;
using ZARI.Application.Features.Hr.Positions.Delete;
using ZARI.Application.Features.Hr.Positions.Get;
using ZARI.Application.Features.Hr.Positions.GetAll;
using ZARI.Application.Features.Hr.Positions.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class PositionEndpoints
{
    public static void MapPositionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/positions")
            .WithTags("Positions")
            .WithGroupName("HR")
            .RequireAuthorization();

        group.MapGet("/", GetAll).WithName("GetAllPositions").WithSummary("Get all positions");
        group.MapGet("/{id:guid}", GetById).WithName("GetPositionById").WithSummary("Get a position by ID");
        group.MapPost("/", Create).AddEndpointFilter<ValidationFilter<CreatePositionCommand>>().WithName("CreatePosition").WithSummary("Create a new position");
        group.MapPut("/{id:guid}", Update).WithName("UpdatePosition").WithSummary("Update an existing position");
        group.MapDelete("/{id:guid}", Delete).WithName("DeletePosition").WithSummary("Delete a position");
    }

    private static async Task<IResult> GetAll(IQueryHandler<GetAllPositionsQuery, Result<List<PositionResponse>>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllPositionsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(Guid id, IQueryHandler<GetPositionQuery, Result<PositionResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetPositionQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(CreatePositionCommand command, ICommandHandler<CreatePositionCommand, Result<PositionResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.CreatedAtRoute(result.Value, "GetPositionById", new { id = result.Value!.Id }) : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(Guid id, UpdatePositionRequest request, IValidator<UpdatePositionCommand> validator, ICommandHandler<UpdatePositionCommand, Result> handler, CancellationToken cancellationToken)
    {
        var command = new UpdatePositionCommand(id, request.Code, request.Name, request.DepartmentId, request.Status, request.PayGradeMin, request.PayGradeMax);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(Guid id, ICommandHandler<DeletePositionCommand, Result> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeletePositionCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdatePositionRequest(string Code, string Name, Guid DepartmentId, string Status, decimal? PayGradeMin, decimal? PayGradeMax);
