using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockAdjustments.ApproveCancellation;
using ZARI.Application.Features.Inventory.StockAdjustments.Approve;
using ZARI.Application.Features.Inventory.StockAdjustments.Cancel;
using ZARI.Application.Features.Inventory.StockAdjustments.Create;
using ZARI.Application.Features.Inventory.StockAdjustments.Delete;
using ZARI.Application.Features.Inventory.StockAdjustments.Get;
using ZARI.Application.Features.Inventory.StockAdjustments.GetAll;
using ZARI.Application.Features.Inventory.StockAdjustments.Reject;
using ZARI.Application.Features.Inventory.StockAdjustments.RejectCancellation;
using ZARI.Application.Features.Inventory.StockAdjustments.RequestCancellation;
using ZARI.Application.Features.Inventory.StockAdjustments.Submit;
using ZARI.Application.Features.Inventory.StockAdjustments.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class StockAdjustmentEndpoints
{
    public static void MapStockAdjustmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stock-adjustments")
            .WithTags("StockAdjustments")
            .WithGroupName("Inventory")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllStockAdjustments")
            .WithSummary("Get all stock adjustments");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetStockAdjustmentById")
            .WithSummary("Get a stock adjustment by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateStockAdjustmentCommand>>()
            .WithName("CreateStockAdjustment")
            .WithSummary("Create a draft stock adjustment");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateStockAdjustment")
            .WithSummary("Update a draft stock adjustment");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteStockAdjustment")
            .WithSummary("Delete a draft stock adjustment");

        group.MapPost("/{id:guid}/submit", Submit)
            .WithName("SubmitStockAdjustment")
            .WithSummary("Submit a draft stock adjustment for approval");

        group.MapPost("/{id:guid}/approve", Approve)
            .WithName("ApproveStockAdjustment")
            .WithSummary("Approve a pending stock adjustment — posts stock, serials, and the GL journal");

        group.MapPost("/{id:guid}/reject", Reject)
            .WithName("RejectStockAdjustment")
            .WithSummary("Reject a pending stock adjustment back to draft");

        group.MapPost("/{id:guid}/cancel", Cancel)
            .WithName("CancelStockAdjustment")
            .WithSummary("Cancel a draft or pending-approval stock adjustment directly");

        group.MapPost("/{id:guid}/request-cancellation", RequestCancellation)
            .WithName("RequestStockAdjustmentCancellation")
            .WithSummary("Request cancellation of a posted stock adjustment");

        group.MapPost("/{id:guid}/approve-cancellation", ApproveCancellation)
            .WithName("ApproveStockAdjustmentCancellation")
            .WithSummary("Approve a cancellation request — reverses stock, serials, and the GL journal");

        group.MapPost("/{id:guid}/reject-cancellation", RejectCancellation)
            .WithName("RejectStockAdjustmentCancellation")
            .WithSummary("Reject a cancellation request — the document stands as posted");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllStockAdjustmentsQuery, Result<List<StockAdjustmentResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllStockAdjustmentsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetStockAdjustmentQuery, Result<StockAdjustmentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetStockAdjustmentQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateStockAdjustmentCommand command,
        ICommandHandler<CreateStockAdjustmentCommand, Result<StockAdjustmentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetStockAdjustmentById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateStockAdjustmentRequest request,
        IValidator<UpdateStockAdjustmentCommand> validator,
        ICommandHandler<UpdateStockAdjustmentCommand, Result<StockAdjustmentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateStockAdjustmentCommand(
            id, request.BranchId, request.WarehouseId, request.AdjustmentDate, request.ReasonCode, request.Remarks, request.UpdatedBy, request.Lines);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteStockAdjustmentCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteStockAdjustmentCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Submit(
        Guid id,
        SubmitStockAdjustmentRequest request,
        IValidator<SubmitStockAdjustmentCommand> validator,
        ICommandHandler<SubmitStockAdjustmentCommand, Result<StockAdjustmentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new SubmitStockAdjustmentCommand(id, request.RequestedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Approve(
        Guid id,
        DecideStockAdjustmentRequest request,
        IValidator<ApproveStockAdjustmentCommand> validator,
        ICommandHandler<ApproveStockAdjustmentCommand, Result<StockAdjustmentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveStockAdjustmentCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Reject(
        Guid id,
        DecideStockAdjustmentRequiredCommentRequest request,
        IValidator<RejectStockAdjustmentCommand> validator,
        ICommandHandler<RejectStockAdjustmentCommand, Result<StockAdjustmentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectStockAdjustmentCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Cancel(
        Guid id,
        CancelStockAdjustmentRequest request,
        IValidator<CancelStockAdjustmentCommand> validator,
        ICommandHandler<CancelStockAdjustmentCommand, Result<StockAdjustmentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CancelStockAdjustmentCommand(id, request.CancelledBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RequestCancellation(
        Guid id,
        RequestStockAdjustmentCancellationRequest request,
        IValidator<RequestStockAdjustmentCancellationCommand> validator,
        ICommandHandler<RequestStockAdjustmentCancellationCommand, Result<StockAdjustmentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RequestStockAdjustmentCancellationCommand(id, request.RequestedBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> ApproveCancellation(
        Guid id,
        DecideStockAdjustmentRequest request,
        IValidator<ApproveStockAdjustmentCancellationCommand> validator,
        ICommandHandler<ApproveStockAdjustmentCancellationCommand, Result<StockAdjustmentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveStockAdjustmentCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RejectCancellation(
        Guid id,
        DecideStockAdjustmentRequiredCommentRequest request,
        IValidator<RejectStockAdjustmentCancellationCommand> validator,
        ICommandHandler<RejectStockAdjustmentCancellationCommand, Result<StockAdjustmentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectStockAdjustmentCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdateStockAdjustmentRequest(
    string BranchId,
    Guid WarehouseId,
    DateTimeOffset AdjustmentDate,
    string? ReasonCode,
    string? Remarks,
    string? UpdatedBy,
    List<StockAdjustmentLineInput> Lines);

public sealed record SubmitStockAdjustmentRequest(string RequestedBy);
public sealed record DecideStockAdjustmentRequest(string ApproverUserId, string? Comments);
public sealed record DecideStockAdjustmentRequiredCommentRequest(string ApproverUserId, string Comments);
public sealed record CancelStockAdjustmentRequest(string CancelledBy, string Reason);
public sealed record RequestStockAdjustmentCancellationRequest(string RequestedBy, string Reason);
