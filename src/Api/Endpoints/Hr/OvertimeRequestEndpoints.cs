using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.OvertimeRequests.Approve;
using ZARI.Application.Features.Hr.OvertimeRequests.Create;
using ZARI.Application.Features.Hr.OvertimeRequests.Delete;
using ZARI.Application.Features.Hr.OvertimeRequests.Get;
using ZARI.Application.Features.Hr.OvertimeRequests.GetAll;
using ZARI.Application.Features.Hr.OvertimeRequests.Reject;
using ZARI.Application.Features.Hr.OvertimeRequests.Submit;
using ZARI.Application.Features.Hr.OvertimeRequests.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class OvertimeRequestEndpoints
{
    public static void MapOvertimeRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/overtime-requests")
            .WithTags("OvertimeRequests")
            .WithGroupName("HR")
            .RequireAuthorization();

        group.MapGet("/", GetAll).WithName("GetAllOvertimeRequests").WithSummary("Get all overtime requests");
        group.MapGet("/{id:guid}", GetById).WithName("GetOvertimeRequestById").WithSummary("Get an overtime request by ID");
        group.MapPost("/", Create).AddEndpointFilter<ValidationFilter<CreateOvertimeRequestCommand>>().WithName("CreateOvertimeRequest").WithSummary("Create a new draft overtime request");
        group.MapPut("/{id:guid}", Update).WithName("UpdateOvertimeRequest").WithSummary("Update an existing draft overtime request");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteOvertimeRequest").WithSummary("Delete a draft overtime request");
        group.MapPost("/{id:guid}/submit", Submit).WithName("SubmitOvertimeRequest").WithSummary("Submit a draft overtime request for approval");
        group.MapPost("/{id:guid}/approve", Approve).WithName("ApproveOvertimeRequest").WithSummary("Approve a pending overtime request");
        group.MapPost("/{id:guid}/reject", Reject).WithName("RejectOvertimeRequest").WithSummary("Reject a pending overtime request back to draft");
    }

    private static async Task<IResult> GetAll(IQueryHandler<GetAllOvertimeRequestsQuery, Result<List<OvertimeRequestResponse>>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllOvertimeRequestsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(Guid id, IQueryHandler<GetOvertimeRequestQuery, Result<OvertimeRequestResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetOvertimeRequestQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(CreateOvertimeRequestCommand command, ICommandHandler<CreateOvertimeRequestCommand, Result<OvertimeRequestResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.CreatedAtRoute(result.Value, "GetOvertimeRequestById", new { id = result.Value!.Id }) : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(Guid id, UpdateOvertimeRequestRequest request, IValidator<UpdateOvertimeRequestCommand> validator, ICommandHandler<UpdateOvertimeRequestCommand, Result> handler, CancellationToken cancellationToken)
    {
        var command = new UpdateOvertimeRequestCommand(id, request.Date, request.PlannedTimeFrom, request.PlannedTimeTo, request.OtType, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(Guid id, ICommandHandler<DeleteOvertimeRequestCommand, Result> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteOvertimeRequestCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Submit(Guid id, SubmitOvertimeRequestRequest request, IValidator<SubmitOvertimeRequestCommand> validator, ICommandHandler<SubmitOvertimeRequestCommand, Result<OvertimeRequestResponse>> handler, CancellationToken cancellationToken)
    {
        var command = new SubmitOvertimeRequestCommand(id, request.RequestedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Approve(Guid id, DecideOvertimeRequestRequest request, IValidator<ApproveOvertimeRequestCommand> validator, ICommandHandler<ApproveOvertimeRequestCommand, Result<OvertimeRequestResponse>> handler, CancellationToken cancellationToken)
    {
        var command = new ApproveOvertimeRequestCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Reject(Guid id, DecideOvertimeRequestRequiredCommentRequest request, IValidator<RejectOvertimeRequestCommand> validator, ICommandHandler<RejectOvertimeRequestCommand, Result<OvertimeRequestResponse>> handler, CancellationToken cancellationToken)
    {
        var command = new RejectOvertimeRequestCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdateOvertimeRequestRequest(DateTimeOffset Date, DateTimeOffset PlannedTimeFrom, DateTimeOffset PlannedTimeTo, string OtType, string? Reason);
public sealed record SubmitOvertimeRequestRequest(string RequestedBy);
public sealed record DecideOvertimeRequestRequest(string ApproverUserId, string? Comments);
public sealed record DecideOvertimeRequestRequiredCommentRequest(string ApproverUserId, string Comments);
