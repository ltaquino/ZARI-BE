using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Approve;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Compute;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Create;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Delete;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Get;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.GetAll;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Reject;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Submit;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class ThirteenthMonthPayRunEndpoints
{
    public static void MapThirteenthMonthPayRunEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/thirteenth-month-pay-runs")
            .WithTags("ThirteenthMonthPayRuns")
            .WithGroupName("HR")
            .RequireAuthorization();

        group.MapGet("/", GetAll).WithName("GetAllThirteenthMonthPayRuns").WithSummary("Get all 13th month pay runs");
        group.MapGet("/{id:guid}", GetById).WithName("GetThirteenthMonthPayRunById").WithSummary("Get a 13th month pay run by ID, including its lines");
        group.MapPost("/", Create).AddEndpointFilter<ValidationFilter<CreateThirteenthMonthPayRunCommand>>().WithName("CreateThirteenthMonthPayRun").WithSummary("Create a new draft 13th month pay run for a branch/year");
        group.MapPut("/{id:guid}", Update).WithName("UpdateThirteenthMonthPayRun").WithSummary("Update an existing draft 13th month pay run's pay date");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteThirteenthMonthPayRun").WithSummary("Delete a draft 13th month pay run");
        group.MapPost("/{id:guid}/compute", Compute).WithName("ComputeThirteenthMonthPayRun").WithSummary("(Re)generate this draft 13th month pay run's lines");
        group.MapPost("/{id:guid}/submit", Submit).WithName("SubmitThirteenthMonthPayRun").WithSummary("Submit a draft 13th month pay run for approval");
        group.MapPost("/{id:guid}/approve", Approve).WithName("ApproveThirteenthMonthPayRun").WithSummary("Approve a pending 13th month pay run — posts its aggregated GL journal");
        group.MapPost("/{id:guid}/reject", Reject).WithName("RejectThirteenthMonthPayRun").WithSummary("Reject a pending 13th month pay run back to draft");
    }

    private static async Task<IResult> GetAll(IQueryHandler<GetAllThirteenthMonthPayRunsQuery, Result<List<ThirteenthMonthPayRunSummaryResponse>>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllThirteenthMonthPayRunsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(Guid id, IQueryHandler<GetThirteenthMonthPayRunQuery, Result<ThirteenthMonthPayRunResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetThirteenthMonthPayRunQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(CreateThirteenthMonthPayRunCommand command, ICommandHandler<CreateThirteenthMonthPayRunCommand, Result<ThirteenthMonthPayRunResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.CreatedAtRoute(result.Value, "GetThirteenthMonthPayRunById", new { id = result.Value!.Id }) : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(Guid id, UpdateThirteenthMonthPayRunRequest request, IValidator<UpdateThirteenthMonthPayRunCommand> validator, ICommandHandler<UpdateThirteenthMonthPayRunCommand, Result> handler, CancellationToken cancellationToken)
    {
        var command = new UpdateThirteenthMonthPayRunCommand(id, request.PayDate);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(Guid id, ICommandHandler<DeleteThirteenthMonthPayRunCommand, Result> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteThirteenthMonthPayRunCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Compute(Guid id, ICommandHandler<ComputeThirteenthMonthPayRunCommand, Result<ThirteenthMonthPayRunResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ComputeThirteenthMonthPayRunCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Submit(Guid id, SubmitThirteenthMonthPayRunRequest request, IValidator<SubmitThirteenthMonthPayRunCommand> validator, ICommandHandler<SubmitThirteenthMonthPayRunCommand, Result<ThirteenthMonthPayRunResponse>> handler, CancellationToken cancellationToken)
    {
        var command = new SubmitThirteenthMonthPayRunCommand(id, request.RequestedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Approve(Guid id, DecideThirteenthMonthPayRunRequest request, IValidator<ApproveThirteenthMonthPayRunCommand> validator, ICommandHandler<ApproveThirteenthMonthPayRunCommand, Result<ThirteenthMonthPayRunResponse>> handler, CancellationToken cancellationToken)
    {
        var command = new ApproveThirteenthMonthPayRunCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Reject(Guid id, DecideThirteenthMonthPayRunRequiredCommentRequest request, IValidator<RejectThirteenthMonthPayRunCommand> validator, ICommandHandler<RejectThirteenthMonthPayRunCommand, Result<ThirteenthMonthPayRunResponse>> handler, CancellationToken cancellationToken)
    {
        var command = new RejectThirteenthMonthPayRunCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdateThirteenthMonthPayRunRequest(DateTimeOffset PayDate);
public sealed record SubmitThirteenthMonthPayRunRequest(string RequestedBy);
public sealed record DecideThirteenthMonthPayRunRequest(string ApproverUserId, string? Comments);
public sealed record DecideThirteenthMonthPayRunRequiredCommentRequest(string ApproverUserId, string Comments);
