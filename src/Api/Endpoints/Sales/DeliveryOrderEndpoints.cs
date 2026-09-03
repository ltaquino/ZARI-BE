using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.DeliveryOrders.ApproveCancellation;
using ZARI.Application.Features.Sales.DeliveryOrders.Approve;
using ZARI.Application.Features.Sales.DeliveryOrders.Cancel;
using ZARI.Application.Features.Sales.DeliveryOrders.Create;
using ZARI.Application.Features.Sales.DeliveryOrders.Delete;
using ZARI.Application.Features.Sales.DeliveryOrders.Get;
using ZARI.Application.Features.Sales.DeliveryOrders.GetAll;
using ZARI.Application.Features.Sales.DeliveryOrders.GetAllPaged;
using ZARI.Application.Features.Sales.DeliveryOrders.Reject;
using ZARI.Application.Features.Sales.DeliveryOrders.RejectCancellation;
using ZARI.Application.Features.Sales.DeliveryOrders.RequestCancellation;
using ZARI.Application.Features.Sales.DeliveryOrders.Submit;
using ZARI.Application.Features.Sales.DeliveryOrders.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class DeliveryOrderEndpoints
{
    public static void MapDeliveryOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/delivery-orders")
            .WithTags("DeliveryOrders")
            .WithGroupName("Sales")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllDeliveryOrders")
            .WithSummary("Get all deliveries");

        group.MapGet("/paged", GetAllPaged)
            .WithName("GetAllDeliveryOrdersPaged")
            .WithSummary("Get a page of deliveries, optionally filtered by search text");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetDeliveryOrderById")
            .WithSummary("Get a delivery by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateDeliveryOrderCommand>>()
            .WithName("CreateDeliveryOrder")
            .WithSummary("Create a draft delivery (or post it directly, issuing stock and booking COGS, if quick-post is enabled)");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateDeliveryOrder")
            .WithSummary("Update a draft delivery");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteDeliveryOrder")
            .WithSummary("Delete a draft delivery");

        group.MapPost("/{id:guid}/submit", Submit)
            .WithName("SubmitDeliveryOrder")
            .WithSummary("Submit a draft delivery for approval");

        group.MapPost("/{id:guid}/approve", Approve)
            .WithName("ApproveDeliveryOrder")
            .WithSummary("Approve a pending delivery — issues stock and books COGS/Inventory");

        group.MapPost("/{id:guid}/reject", Reject)
            .WithName("RejectDeliveryOrder")
            .WithSummary("Reject a pending delivery back to draft");

        group.MapPost("/{id:guid}/cancel", Cancel)
            .WithName("CancelDeliveryOrder")
            .WithSummary("Cancel a draft or pending-approval delivery directly");

        group.MapPost("/{id:guid}/request-cancellation", RequestCancellation)
            .WithName("RequestDeliveryOrderCancellation")
            .WithSummary("Request cancellation of a posted delivery");

        group.MapPost("/{id:guid}/approve-cancellation", ApproveCancellation)
            .WithName("ApproveDeliveryOrderCancellation")
            .WithSummary("Approve a cancellation request — reverses the stock and GL posting");

        group.MapPost("/{id:guid}/reject-cancellation", RejectCancellation)
            .WithName("RejectDeliveryOrderCancellation")
            .WithSummary("Reject a cancellation request — the document stands as posted");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllDeliveryOrdersQuery, Result<List<DeliveryOrderResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllDeliveryOrdersQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetAllPaged(
        int? page,
        int? pageSize,
        string? search,
        IQueryHandler<GetAllDeliveryOrdersPagedQuery, Result<PagedResult<DeliveryOrderResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllDeliveryOrdersPagedQuery(page ?? 1, pageSize ?? 20, search), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetDeliveryOrderQuery, Result<DeliveryOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetDeliveryOrderQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateDeliveryOrderCommand command,
        ICommandHandler<CreateDeliveryOrderCommand, Result<DeliveryOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetDeliveryOrderById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateDeliveryOrderRequest request,
        IValidator<UpdateDeliveryOrderCommand> validator,
        ICommandHandler<UpdateDeliveryOrderCommand, Result<DeliveryOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateDeliveryOrderCommand(
            id, request.BranchId, request.WarehouseId, request.CustomerId, request.SalesOrderId, request.DeliveryDate,
            request.Remarks, request.CostCenterId, request.UpdatedBy, request.Lines);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteDeliveryOrderCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteDeliveryOrderCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Submit(
        Guid id,
        SubmitDeliveryOrderRequest request,
        IValidator<SubmitDeliveryOrderCommand> validator,
        ICommandHandler<SubmitDeliveryOrderCommand, Result<DeliveryOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new SubmitDeliveryOrderCommand(id, request.RequestedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Approve(
        Guid id,
        DecideDeliveryOrderRequest request,
        IValidator<ApproveDeliveryOrderCommand> validator,
        ICommandHandler<ApproveDeliveryOrderCommand, Result<DeliveryOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveDeliveryOrderCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Reject(
        Guid id,
        DecideDeliveryOrderRequiredCommentRequest request,
        IValidator<RejectDeliveryOrderCommand> validator,
        ICommandHandler<RejectDeliveryOrderCommand, Result<DeliveryOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectDeliveryOrderCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Cancel(
        Guid id,
        CancelDeliveryOrderRequest request,
        IValidator<CancelDeliveryOrderCommand> validator,
        ICommandHandler<CancelDeliveryOrderCommand, Result<DeliveryOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CancelDeliveryOrderCommand(id, request.CancelledBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RequestCancellation(
        Guid id,
        RequestDeliveryOrderCancellationRequest request,
        IValidator<RequestDeliveryOrderCancellationCommand> validator,
        ICommandHandler<RequestDeliveryOrderCancellationCommand, Result<DeliveryOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RequestDeliveryOrderCancellationCommand(id, request.RequestedBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> ApproveCancellation(
        Guid id,
        DecideDeliveryOrderRequest request,
        IValidator<ApproveDeliveryOrderCancellationCommand> validator,
        ICommandHandler<ApproveDeliveryOrderCancellationCommand, Result<DeliveryOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveDeliveryOrderCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RejectCancellation(
        Guid id,
        DecideDeliveryOrderRequiredCommentRequest request,
        IValidator<RejectDeliveryOrderCancellationCommand> validator,
        ICommandHandler<RejectDeliveryOrderCancellationCommand, Result<DeliveryOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectDeliveryOrderCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdateDeliveryOrderRequest(
    string BranchId,
    Guid WarehouseId,
    Guid CustomerId,
    Guid? SalesOrderId,
    DateTimeOffset DeliveryDate,
    string? Remarks,
    Guid? CostCenterId,
    string? UpdatedBy,
    List<DeliveryOrderLineInput> Lines);

// DeliveryOrderLineInput is defined in CreateDeliveryOrderCommand.cs and reused as-is for Update —
// same convention as SalesOrderLineInput/PurchaseOrderLineInput.

public sealed record SubmitDeliveryOrderRequest(string RequestedBy);
public sealed record DecideDeliveryOrderRequest(string ApproverUserId, string? Comments);
public sealed record DecideDeliveryOrderRequiredCommentRequest(string ApproverUserId, string Comments);
public sealed record CancelDeliveryOrderRequest(string CancelledBy, string Reason);
public sealed record RequestDeliveryOrderCancellationRequest(string RequestedBy, string Reason);
