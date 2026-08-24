using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class ApprovalRequestEndpoints
{
    public static void MapApprovalRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/approval-requests")
            .WithTags("ApprovalRequests")
            .WithGroupName("Workflow")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllApprovalRequests")
            .WithSummary("Get every approval request, with its decision history");

        group.MapPost("/", Submit)
            .AddEndpointFilter<ValidationFilter<SubmitForApprovalCommand>>()
            .WithName("SubmitForApproval")
            .WithSummary("Submit an entity for approval (or request cancellation of a posted one)");

        group.MapPost("/{id:guid}/decide", Decide)
            .WithName("DecideApprovalRequest")
            .WithSummary("Approve or reject a pending approval request");

        group.MapPost("/cancel-pending", CancelPending)
            .AddEndpointFilter<ValidationFilter<CancelPendingApprovalRequestCommand>>()
            .WithName("CancelPendingApprovalRequest")
            .WithSummary("Cancel the latest pending approval request for an entity, if any");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllApprovalRequestsQuery, Result<List<ApprovalRequestResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllApprovalRequestsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Submit(
        SubmitForApprovalCommand command,
        ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Decide(
        Guid id,
        DecideApprovalRequestRequest request,
        IValidator<DecideApprovalRequestCommand> validator,
        ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new DecideApprovalRequestCommand(id, request.ApproverUserId, request.Action, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> CancelPending(
        CancelPendingApprovalRequestCommand command,
        ICommandHandler<CancelPendingApprovalRequestCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record DecideApprovalRequestRequest(string ApproverUserId, string Action, string? Comments);
