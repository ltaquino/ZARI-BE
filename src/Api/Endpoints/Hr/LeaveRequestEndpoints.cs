using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.LeaveRequests.Approve;
using ZARI.Application.Features.Hr.LeaveRequests.Create;
using ZARI.Application.Features.Hr.LeaveRequests.Delete;
using ZARI.Application.Features.Hr.LeaveRequests.Get;
using ZARI.Application.Features.Hr.LeaveRequests.GetAll;
using ZARI.Application.Features.Hr.LeaveRequests.Reject;
using ZARI.Application.Features.Hr.LeaveRequests.Submit;
using ZARI.Application.Features.Hr.LeaveRequests.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class LeaveRequestEndpoints
{
    public static void MapLeaveRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/leave-requests")
            .WithTags("LeaveRequests")
            .WithGroupName("HR")
            .RequireAuthorization();

        group.MapGet("/", GetAll).WithName("GetAllLeaveRequests").WithSummary("Get all leave requests");
        group.MapGet("/{id:guid}", GetById).WithName("GetLeaveRequestById").WithSummary("Get a leave request by ID");
        group.MapPost("/", Create).AddEndpointFilter<ValidationFilter<CreateLeaveRequestCommand>>().WithName("CreateLeaveRequest").WithSummary("Create a new draft leave request");
        group.MapPut("/{id:guid}", Update).WithName("UpdateLeaveRequest").WithSummary("Update an existing draft leave request");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteLeaveRequest").WithSummary("Delete a draft leave request");
        group.MapPost("/{id:guid}/submit", Submit).WithName("SubmitLeaveRequest").WithSummary("Submit a draft leave request for approval");
        group.MapPost("/{id:guid}/approve", Approve).WithName("ApproveLeaveRequest").WithSummary("Approve a pending leave request");
        group.MapPost("/{id:guid}/reject", Reject).WithName("RejectLeaveRequest").WithSummary("Reject a pending leave request back to draft");
    }

    private static async Task<IResult> GetAll(IQueryHandler<GetAllLeaveRequestsQuery, Result<List<LeaveRequestResponse>>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllLeaveRequestsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(Guid id, IQueryHandler<GetLeaveRequestQuery, Result<LeaveRequestResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetLeaveRequestQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(CreateLeaveRequestCommand command, ICommandHandler<CreateLeaveRequestCommand, Result<LeaveRequestResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.CreatedAtRoute(result.Value, "GetLeaveRequestById", new { id = result.Value!.Id }) : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(Guid id, UpdateLeaveRequestRequest request, IValidator<UpdateLeaveRequestCommand> validator, ICommandHandler<UpdateLeaveRequestCommand, Result> handler, CancellationToken cancellationToken)
    {
        var command = new UpdateLeaveRequestCommand(id, request.LeaveTypeId, request.DateFrom, request.DateTo, request.Reason, request.AttachmentUrl);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(Guid id, ICommandHandler<DeleteLeaveRequestCommand, Result> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteLeaveRequestCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Submit(Guid id, SubmitLeaveRequestRequest request, IValidator<SubmitLeaveRequestCommand> validator, ICommandHandler<SubmitLeaveRequestCommand, Result<LeaveRequestResponse>> handler, CancellationToken cancellationToken)
    {
        var command = new SubmitLeaveRequestCommand(id, request.RequestedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Approve(Guid id, DecideLeaveRequestRequest request, IValidator<ApproveLeaveRequestCommand> validator, ICommandHandler<ApproveLeaveRequestCommand, Result<LeaveRequestResponse>> handler, CancellationToken cancellationToken)
    {
        var command = new ApproveLeaveRequestCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Reject(Guid id, DecideLeaveRequestRequiredCommentRequest request, IValidator<RejectLeaveRequestCommand> validator, ICommandHandler<RejectLeaveRequestCommand, Result<LeaveRequestResponse>> handler, CancellationToken cancellationToken)
    {
        var command = new RejectLeaveRequestCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdateLeaveRequestRequest(Guid LeaveTypeId, DateTimeOffset DateFrom, DateTimeOffset DateTo, string? Reason, string? AttachmentUrl);
public sealed record SubmitLeaveRequestRequest(string RequestedBy);
public sealed record DecideLeaveRequestRequest(string ApproverUserId, string? Comments);
public sealed record DecideLeaveRequestRequiredCommentRequest(string ApproverUserId, string Comments);
