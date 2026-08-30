using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesOrders.ApproveCancellation;
using ZARI.Application.Features.Sales.SalesOrders.Approve;
using ZARI.Application.Features.Sales.SalesOrders.Cancel;
using ZARI.Application.Features.Sales.SalesOrders.Create;
using ZARI.Application.Features.Sales.SalesOrders.Delete;
using ZARI.Application.Features.Sales.SalesOrders.Get;
using ZARI.Application.Features.Sales.SalesOrders.GetAll;
using ZARI.Application.Features.Sales.SalesOrders.Reject;
using ZARI.Application.Features.Sales.SalesOrders.RejectCancellation;
using ZARI.Application.Features.Sales.SalesOrders.RequestCancellation;
using ZARI.Application.Features.Sales.SalesOrders.Submit;
using ZARI.Application.Features.Sales.SalesOrders.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class SalesOrderEndpoints
{
    public static void MapSalesOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sales-orders")
            .WithTags("SalesOrders")
            .WithGroupName("Sales")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllSalesOrders")
            .WithSummary("Get all sales orders");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetSalesOrderById")
            .WithSummary("Get a sales order by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateSalesOrderCommand>>()
            .WithName("CreateSalesOrder")
            .WithSummary("Create a draft sales order (or post it directly if quick-post is enabled)");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateSalesOrder")
            .WithSummary("Update a draft sales order");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteSalesOrder")
            .WithSummary("Delete a draft sales order");

        group.MapPost("/{id:guid}/submit", Submit)
            .WithName("SubmitSalesOrder")
            .WithSummary("Submit a draft sales order for approval");

        group.MapPost("/{id:guid}/approve", Approve)
            .WithName("ApproveSalesOrder")
            .WithSummary("Approve a pending sales order");

        group.MapPost("/{id:guid}/reject", Reject)
            .WithName("RejectSalesOrder")
            .WithSummary("Reject a pending sales order back to draft");

        group.MapPost("/{id:guid}/cancel", Cancel)
            .WithName("CancelSalesOrder")
            .WithSummary("Cancel a draft or pending-approval sales order directly");

        group.MapPost("/{id:guid}/request-cancellation", RequestCancellation)
            .WithName("RequestSalesOrderCancellation")
            .WithSummary("Request cancellation of a posted sales order");

        group.MapPost("/{id:guid}/approve-cancellation", ApproveCancellation)
            .WithName("ApproveSalesOrderCancellation")
            .WithSummary("Approve a cancellation request");

        group.MapPost("/{id:guid}/reject-cancellation", RejectCancellation)
            .WithName("RejectSalesOrderCancellation")
            .WithSummary("Reject a cancellation request — the document stands as posted");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllSalesOrdersQuery, Result<List<SalesOrderResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllSalesOrdersQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetSalesOrderQuery, Result<SalesOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetSalesOrderQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateSalesOrderCommand command,
        ICommandHandler<CreateSalesOrderCommand, Result<SalesOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetSalesOrderById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateSalesOrderRequest request,
        IValidator<UpdateSalesOrderCommand> validator,
        ICommandHandler<UpdateSalesOrderCommand, Result<SalesOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateSalesOrderCommand(
            id, request.BranchId, request.CustomerId, request.OrderDate, request.ExpectedDeliveryDate, request.Remarks, request.DiscountPct, request.UpdatedBy, request.Lines);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteSalesOrderCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteSalesOrderCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Submit(
        Guid id,
        SubmitSalesOrderRequest request,
        IValidator<SubmitSalesOrderCommand> validator,
        ICommandHandler<SubmitSalesOrderCommand, Result<SalesOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new SubmitSalesOrderCommand(id, request.RequestedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Approve(
        Guid id,
        DecideSalesOrderRequest request,
        IValidator<ApproveSalesOrderCommand> validator,
        ICommandHandler<ApproveSalesOrderCommand, Result<SalesOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveSalesOrderCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Reject(
        Guid id,
        DecideSalesOrderRequiredCommentRequest request,
        IValidator<RejectSalesOrderCommand> validator,
        ICommandHandler<RejectSalesOrderCommand, Result<SalesOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectSalesOrderCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Cancel(
        Guid id,
        CancelSalesOrderRequest request,
        IValidator<CancelSalesOrderCommand> validator,
        ICommandHandler<CancelSalesOrderCommand, Result<SalesOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CancelSalesOrderCommand(id, request.CancelledBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RequestCancellation(
        Guid id,
        RequestSalesOrderCancellationRequest request,
        IValidator<RequestSalesOrderCancellationCommand> validator,
        ICommandHandler<RequestSalesOrderCancellationCommand, Result<SalesOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RequestSalesOrderCancellationCommand(id, request.RequestedBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> ApproveCancellation(
        Guid id,
        DecideSalesOrderRequest request,
        IValidator<ApproveSalesOrderCancellationCommand> validator,
        ICommandHandler<ApproveSalesOrderCancellationCommand, Result<SalesOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveSalesOrderCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RejectCancellation(
        Guid id,
        DecideSalesOrderRequiredCommentRequest request,
        IValidator<RejectSalesOrderCancellationCommand> validator,
        ICommandHandler<RejectSalesOrderCancellationCommand, Result<SalesOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectSalesOrderCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdateSalesOrderRequest(
    string BranchId,
    Guid CustomerId,
    DateTimeOffset OrderDate,
    DateTimeOffset? ExpectedDeliveryDate,
    string? Remarks,
    decimal? DiscountPct,
    string? UpdatedBy,
    List<SalesOrderLineInput> Lines);

// SalesOrderLineInput is defined in CreateSalesOrderCommand.cs and reused as-is for Update — same
// convention as PurchaseOrderLineInput.

public sealed record SubmitSalesOrderRequest(string RequestedBy);
public sealed record DecideSalesOrderRequest(string ApproverUserId, string? Comments);
public sealed record DecideSalesOrderRequiredCommentRequest(string ApproverUserId, string Comments);
public sealed record CancelSalesOrderRequest(string CancelledBy, string Reason);
public sealed record RequestSalesOrderCancellationRequest(string RequestedBy, string Reason);
