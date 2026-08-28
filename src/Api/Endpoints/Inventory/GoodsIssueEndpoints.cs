using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsIssues.ApproveCancellation;
using ZARI.Application.Features.Inventory.GoodsIssues.Approve;
using ZARI.Application.Features.Inventory.GoodsIssues.Cancel;
using ZARI.Application.Features.Inventory.GoodsIssues.Create;
using ZARI.Application.Features.Inventory.GoodsIssues.Delete;
using ZARI.Application.Features.Inventory.GoodsIssues.Get;
using ZARI.Application.Features.Inventory.GoodsIssues.GetAll;
using ZARI.Application.Features.Inventory.GoodsIssues.MarkDelivered;
using ZARI.Application.Features.Inventory.GoodsIssues.MarkInTransit;
using ZARI.Application.Features.Inventory.GoodsIssues.Reject;
using ZARI.Application.Features.Inventory.GoodsIssues.RejectCancellation;
using ZARI.Application.Features.Inventory.GoodsIssues.RequestCancellation;
using ZARI.Application.Features.Inventory.GoodsIssues.Submit;
using ZARI.Application.Features.Inventory.GoodsIssues.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class GoodsIssueEndpoints
{
    public static void MapGoodsIssueEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/goods-issues")
            .WithTags("GoodsIssues")
            .WithGroupName("Inventory")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllGoodsIssues")
            .WithSummary("Get all goods issues");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetGoodsIssueById")
            .WithSummary("Get a goods issue by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateGoodsIssueCommand>>()
            .WithName("CreateGoodsIssue")
            .WithSummary("Create a draft goods issue");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateGoodsIssue")
            .WithSummary("Update a draft goods issue");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteGoodsIssue")
            .WithSummary("Delete a draft goods issue");

        group.MapPost("/{id:guid}/submit", Submit)
            .WithName("SubmitGoodsIssue")
            .WithSummary("Submit a draft goods issue for approval");

        group.MapPost("/{id:guid}/approve", Approve)
            .WithName("ApproveGoodsIssue")
            .WithSummary("Approve a pending goods issue — issues stock, serials, and posts the GL journal");

        group.MapPost("/{id:guid}/reject", Reject)
            .WithName("RejectGoodsIssue")
            .WithSummary("Reject a pending goods issue back to draft");

        group.MapPost("/{id:guid}/cancel", Cancel)
            .WithName("CancelGoodsIssue")
            .WithSummary("Cancel a draft or pending-approval goods issue directly");

        group.MapPost("/{id:guid}/request-cancellation", RequestCancellation)
            .WithName("RequestGoodsIssueCancellation")
            .WithSummary("Request cancellation of a posted goods issue");

        group.MapPost("/{id:guid}/approve-cancellation", ApproveCancellation)
            .WithName("ApproveGoodsIssueCancellation")
            .WithSummary("Approve a cancellation request — reverses stock, serials, and the GL journal");

        group.MapPost("/{id:guid}/reject-cancellation", RejectCancellation)
            .WithName("RejectGoodsIssueCancellation")
            .WithSummary("Reject a cancellation request — the document stands as posted");

        group.MapPost("/{id:guid}/mark-in-transit", MarkInTransit)
            .WithName("MarkGoodsIssueInTransit")
            .WithSummary("Mark a posted transfer's shipment as in transit");

        group.MapPost("/{id:guid}/mark-delivered", MarkDelivered)
            .WithName("MarkGoodsIssueDelivered")
            .WithSummary("Mark a posted transfer's shipment as delivered");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllGoodsIssuesQuery, Result<List<GoodsIssueResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllGoodsIssuesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetGoodsIssueQuery, Result<GoodsIssueResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetGoodsIssueQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateGoodsIssueCommand command,
        ICommandHandler<CreateGoodsIssueCommand, Result<GoodsIssueResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetGoodsIssueById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateGoodsIssueRequest request,
        IValidator<UpdateGoodsIssueCommand> validator,
        ICommandHandler<UpdateGoodsIssueCommand, Result<GoodsIssueResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateGoodsIssueCommand(
            id, request.BranchId, request.WarehouseId, request.ReferenceType, request.DestBranchId, request.DestWarehouseId,
            request.ReasonCode, request.GiDate, request.Remarks, request.StockTransferRequestRefNo, request.StockTransferRequestId,
            request.CostCenterId, request.UpdatedBy, request.Lines);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteGoodsIssueCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteGoodsIssueCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Submit(
        Guid id,
        SubmitGoodsIssueRequest request,
        IValidator<SubmitGoodsIssueCommand> validator,
        ICommandHandler<SubmitGoodsIssueCommand, Result<GoodsIssueResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new SubmitGoodsIssueCommand(id, request.RequestedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Approve(
        Guid id,
        DecideGoodsIssueRequest request,
        IValidator<ApproveGoodsIssueCommand> validator,
        ICommandHandler<ApproveGoodsIssueCommand, Result<GoodsIssueResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveGoodsIssueCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Reject(
        Guid id,
        DecideGoodsIssueRequiredCommentRequest request,
        IValidator<RejectGoodsIssueCommand> validator,
        ICommandHandler<RejectGoodsIssueCommand, Result<GoodsIssueResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectGoodsIssueCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Cancel(
        Guid id,
        CancelGoodsIssueRequest request,
        IValidator<CancelGoodsIssueCommand> validator,
        ICommandHandler<CancelGoodsIssueCommand, Result<GoodsIssueResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CancelGoodsIssueCommand(id, request.CancelledBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RequestCancellation(
        Guid id,
        RequestGoodsIssueCancellationRequest request,
        IValidator<RequestGoodsIssueCancellationCommand> validator,
        ICommandHandler<RequestGoodsIssueCancellationCommand, Result<GoodsIssueResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RequestGoodsIssueCancellationCommand(id, request.RequestedBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> ApproveCancellation(
        Guid id,
        DecideGoodsIssueRequest request,
        IValidator<ApproveGoodsIssueCancellationCommand> validator,
        ICommandHandler<ApproveGoodsIssueCancellationCommand, Result<GoodsIssueResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveGoodsIssueCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RejectCancellation(
        Guid id,
        DecideGoodsIssueRequiredCommentRequest request,
        IValidator<RejectGoodsIssueCancellationCommand> validator,
        ICommandHandler<RejectGoodsIssueCancellationCommand, Result<GoodsIssueResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectGoodsIssueCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> MarkInTransit(
        Guid id,
        GoodsIssueUserActionRequest request,
        IValidator<MarkGoodsIssueInTransitCommand> validator,
        ICommandHandler<MarkGoodsIssueInTransitCommand, Result<GoodsIssueResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new MarkGoodsIssueInTransitCommand(id, request.UserId);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> MarkDelivered(
        Guid id,
        GoodsIssueUserActionRequest request,
        IValidator<MarkGoodsIssueDeliveredCommand> validator,
        ICommandHandler<MarkGoodsIssueDeliveredCommand, Result<GoodsIssueResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new MarkGoodsIssueDeliveredCommand(id, request.UserId);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdateGoodsIssueRequest(
    string BranchId,
    Guid WarehouseId,
    string ReferenceType,
    string? DestBranchId,
    Guid? DestWarehouseId,
    string? ReasonCode,
    DateTimeOffset GiDate,
    string? Remarks,
    string? StockTransferRequestRefNo,
    string? StockTransferRequestId,
    Guid? CostCenterId,
    string? UpdatedBy,
    List<GoodsIssueLineInput> Lines);

public sealed record SubmitGoodsIssueRequest(string RequestedBy);
public sealed record DecideGoodsIssueRequest(string ApproverUserId, string? Comments);
public sealed record DecideGoodsIssueRequiredCommentRequest(string ApproverUserId, string Comments);
public sealed record CancelGoodsIssueRequest(string CancelledBy, string Reason);
public sealed record RequestGoodsIssueCancellationRequest(string RequestedBy, string Reason);
public sealed record GoodsIssueUserActionRequest(string UserId);
