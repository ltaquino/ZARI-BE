using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseOrders.ApproveCancellation;
using ZARI.Application.Features.Purchasing.PurchaseOrders.Approve;
using ZARI.Application.Features.Purchasing.PurchaseOrders.Cancel;
using ZARI.Application.Features.Purchasing.PurchaseOrders.Create;
using ZARI.Application.Features.Purchasing.PurchaseOrders.Delete;
using ZARI.Application.Features.Purchasing.PurchaseOrders.Get;
using ZARI.Application.Features.Purchasing.PurchaseOrders.GetAll;
using ZARI.Application.Features.Purchasing.PurchaseOrders.Reject;
using ZARI.Application.Features.Purchasing.PurchaseOrders.RejectCancellation;
using ZARI.Application.Features.Purchasing.PurchaseOrders.RequestCancellation;
using ZARI.Application.Features.Purchasing.PurchaseOrders.Submit;
using ZARI.Application.Features.Purchasing.PurchaseOrders.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class PurchaseOrderEndpoints
{
    public static void MapPurchaseOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/purchase-orders")
            .WithTags("PurchaseOrders")
            .WithGroupName("Purchasing")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllPurchaseOrders")
            .WithSummary("Get all purchase orders");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetPurchaseOrderById")
            .WithSummary("Get a purchase order by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreatePurchaseOrderCommand>>()
            .WithName("CreatePurchaseOrder")
            .WithSummary("Create a draft purchase order");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdatePurchaseOrder")
            .WithSummary("Update a draft purchase order");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeletePurchaseOrder")
            .WithSummary("Delete a draft purchase order");

        group.MapPost("/{id:guid}/submit", Submit)
            .WithName("SubmitPurchaseOrder")
            .WithSummary("Submit a draft purchase order for approval");

        group.MapPost("/{id:guid}/approve", Approve)
            .WithName("ApprovePurchaseOrder")
            .WithSummary("Approve a pending purchase order");

        group.MapPost("/{id:guid}/reject", Reject)
            .WithName("RejectPurchaseOrder")
            .WithSummary("Reject a pending purchase order back to draft");

        group.MapPost("/{id:guid}/cancel", Cancel)
            .WithName("CancelPurchaseOrder")
            .WithSummary("Cancel a draft or pending-approval purchase order directly");

        group.MapPost("/{id:guid}/request-cancellation", RequestCancellation)
            .WithName("RequestPurchaseOrderCancellation")
            .WithSummary("Request cancellation of a posted purchase order");

        group.MapPost("/{id:guid}/approve-cancellation", ApproveCancellation)
            .WithName("ApprovePurchaseOrderCancellation")
            .WithSummary("Approve a cancellation request");

        group.MapPost("/{id:guid}/reject-cancellation", RejectCancellation)
            .WithName("RejectPurchaseOrderCancellation")
            .WithSummary("Reject a cancellation request â€” the document stands as posted");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllPurchaseOrdersQuery, Result<List<PurchaseOrderResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllPurchaseOrdersQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetPurchaseOrderQuery, Result<PurchaseOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetPurchaseOrderQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreatePurchaseOrderCommand command,
        ICommandHandler<CreatePurchaseOrderCommand, Result<PurchaseOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetPurchaseOrderById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdatePurchaseOrderRequest request,
        IValidator<UpdatePurchaseOrderCommand> validator,
        ICommandHandler<UpdatePurchaseOrderCommand, Result<PurchaseOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePurchaseOrderCommand(
            id, request.BranchId, request.SupplierId, request.OrderDate, request.ExpectedDate, request.Remarks, request.PurchaseRequestId, request.UpdatedBy, request.Lines);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeletePurchaseOrderCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeletePurchaseOrderCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Submit(
        Guid id,
        SubmitPurchaseOrderRequest request,
        IValidator<SubmitPurchaseOrderCommand> validator,
        ICommandHandler<SubmitPurchaseOrderCommand, Result<PurchaseOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new SubmitPurchaseOrderCommand(id, request.RequestedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Approve(
        Guid id,
        DecidePurchaseOrderRequest request,
        IValidator<ApprovePurchaseOrderCommand> validator,
        ICommandHandler<ApprovePurchaseOrderCommand, Result<PurchaseOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApprovePurchaseOrderCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Reject(
        Guid id,
        DecidePurchaseOrderRequiredCommentRequest request,
        IValidator<RejectPurchaseOrderCommand> validator,
        ICommandHandler<RejectPurchaseOrderCommand, Result<PurchaseOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectPurchaseOrderCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Cancel(
        Guid id,
        CancelPurchaseOrderRequest request,
        IValidator<CancelPurchaseOrderCommand> validator,
        ICommandHandler<CancelPurchaseOrderCommand, Result<PurchaseOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CancelPurchaseOrderCommand(id, request.CancelledBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RequestCancellation(
        Guid id,
        RequestPurchaseOrderCancellationRequest request,
        IValidator<RequestPurchaseOrderCancellationCommand> validator,
        ICommandHandler<RequestPurchaseOrderCancellationCommand, Result<PurchaseOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RequestPurchaseOrderCancellationCommand(id, request.RequestedBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> ApproveCancellation(
        Guid id,
        DecidePurchaseOrderRequest request,
        IValidator<ApprovePurchaseOrderCancellationCommand> validator,
        ICommandHandler<ApprovePurchaseOrderCancellationCommand, Result<PurchaseOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApprovePurchaseOrderCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RejectCancellation(
        Guid id,
        DecidePurchaseOrderRequiredCommentRequest request,
        IValidator<RejectPurchaseOrderCancellationCommand> validator,
        ICommandHandler<RejectPurchaseOrderCancellationCommand, Result<PurchaseOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectPurchaseOrderCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdatePurchaseOrderRequest(
    string BranchId,
    Guid SupplierId,
    DateTimeOffset OrderDate,
    DateTimeOffset? ExpectedDate,
    string? Remarks,
    Guid? PurchaseRequestId,
    string? UpdatedBy,
    List<PurchaseOrderLineInput> Lines);

// PurchaseOrderLineInput (with its new PurchaseRequestLineId field) is defined in
// CreatePurchaseOrderCommand.cs and reused as-is for Update — same convention as every other
// module's Update DTO in this codebase.

public sealed record SubmitPurchaseOrderRequest(string RequestedBy);
public sealed record DecidePurchaseOrderRequest(string ApproverUserId, string? Comments);
public sealed record DecidePurchaseOrderRequiredCommentRequest(string ApproverUserId, string Comments);
public sealed record CancelPurchaseOrderRequest(string CancelledBy, string Reason);
public sealed record RequestPurchaseOrderCancellationRequest(string RequestedBy, string Reason);
