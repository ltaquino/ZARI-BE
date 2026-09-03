using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesReturns.ApproveCancellation;
using ZARI.Application.Features.Sales.SalesReturns.Approve;
using ZARI.Application.Features.Sales.SalesReturns.Cancel;
using ZARI.Application.Features.Sales.SalesReturns.Create;
using ZARI.Application.Features.Sales.SalesReturns.Delete;
using ZARI.Application.Features.Sales.SalesReturns.Get;
using ZARI.Application.Features.Sales.SalesReturns.GetAll;
using ZARI.Application.Features.Sales.SalesReturns.GetAllPaged;
using ZARI.Application.Features.Sales.SalesReturns.Reject;
using ZARI.Application.Features.Sales.SalesReturns.RejectCancellation;
using ZARI.Application.Features.Sales.SalesReturns.RequestCancellation;
using ZARI.Application.Features.Sales.SalesReturns.Submit;
using ZARI.Application.Features.Sales.SalesReturns.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class SalesReturnEndpoints
{
    public static void MapSalesReturnEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sales-returns")
            .WithTags("SalesReturns")
            .WithGroupName("Sales")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllSalesReturns")
            .WithSummary("Get all sales returns");

        group.MapGet("/paged", GetAllPaged)
            .WithName("GetAllSalesReturnsPaged")
            .WithSummary("Get a page of sales returns, optionally filtered by search text");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetSalesReturnById")
            .WithSummary("Get a sales return by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateSalesReturnCommand>>()
            .WithName("CreateSalesReturn")
            .WithSummary("Create a draft sales return (or post it directly — receiving stock back in and reversing COGS/Inventory + AR/Revenue/VAT — if quick-post is enabled)");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateSalesReturn")
            .WithSummary("Update a draft sales return");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteSalesReturn")
            .WithSummary("Delete a draft sales return");

        group.MapPost("/{id:guid}/submit", Submit)
            .WithName("SubmitSalesReturn")
            .WithSummary("Submit a draft sales return for approval");

        group.MapPost("/{id:guid}/approve", Approve)
            .WithName("ApproveSalesReturn")
            .WithSummary("Approve a pending sales return — receives stock back in and posts the combined COGS/Inventory + AR/Revenue/VAT reversal");

        group.MapPost("/{id:guid}/reject", Reject)
            .WithName("RejectSalesReturn")
            .WithSummary("Reject a pending sales return back to draft");

        group.MapPost("/{id:guid}/cancel", Cancel)
            .WithName("CancelSalesReturn")
            .WithSummary("Cancel a draft or pending-approval sales return directly");

        group.MapPost("/{id:guid}/request-cancellation", RequestCancellation)
            .WithName("RequestSalesReturnCancellation")
            .WithSummary("Request cancellation of a posted sales return");

        group.MapPost("/{id:guid}/approve-cancellation", ApproveCancellation)
            .WithName("ApproveSalesReturnCancellation")
            .WithSummary("Approve a cancellation request — reverses the posted GL journal and the stock receipt");

        group.MapPost("/{id:guid}/reject-cancellation", RejectCancellation)
            .WithName("RejectSalesReturnCancellation")
            .WithSummary("Reject a cancellation request — the document stands as posted");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllSalesReturnsQuery, Result<List<SalesReturnResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllSalesReturnsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetAllPaged(
        int? page,
        int? pageSize,
        string? search,
        IQueryHandler<GetAllSalesReturnsPagedQuery, Result<PagedResult<SalesReturnResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllSalesReturnsPagedQuery(page ?? 1, pageSize ?? 20, search), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetSalesReturnQuery, Result<SalesReturnResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetSalesReturnQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateSalesReturnCommand command,
        ICommandHandler<CreateSalesReturnCommand, Result<SalesReturnResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetSalesReturnById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateSalesReturnRequest request,
        IValidator<UpdateSalesReturnCommand> validator,
        ICommandHandler<UpdateSalesReturnCommand, Result<SalesReturnResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateSalesReturnCommand(
            id, request.BranchId, request.WarehouseId, request.CustomerId, request.DeliveryOrderId, request.ReturnDate,
            request.Remarks, request.CostCenterId, request.UpdatedBy, request.Lines);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteSalesReturnCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteSalesReturnCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Submit(
        Guid id,
        SubmitSalesReturnRequest request,
        IValidator<SubmitSalesReturnCommand> validator,
        ICommandHandler<SubmitSalesReturnCommand, Result<SalesReturnResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new SubmitSalesReturnCommand(id, request.RequestedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Approve(
        Guid id,
        DecideSalesReturnRequest request,
        IValidator<ApproveSalesReturnCommand> validator,
        ICommandHandler<ApproveSalesReturnCommand, Result<SalesReturnResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveSalesReturnCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Reject(
        Guid id,
        DecideSalesReturnRequiredCommentRequest request,
        IValidator<RejectSalesReturnCommand> validator,
        ICommandHandler<RejectSalesReturnCommand, Result<SalesReturnResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectSalesReturnCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Cancel(
        Guid id,
        CancelSalesReturnRequest request,
        IValidator<CancelSalesReturnCommand> validator,
        ICommandHandler<CancelSalesReturnCommand, Result<SalesReturnResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CancelSalesReturnCommand(id, request.CancelledBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RequestCancellation(
        Guid id,
        RequestSalesReturnCancellationRequest request,
        IValidator<RequestSalesReturnCancellationCommand> validator,
        ICommandHandler<RequestSalesReturnCancellationCommand, Result<SalesReturnResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RequestSalesReturnCancellationCommand(id, request.RequestedBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> ApproveCancellation(
        Guid id,
        DecideSalesReturnRequest request,
        IValidator<ApproveSalesReturnCancellationCommand> validator,
        ICommandHandler<ApproveSalesReturnCancellationCommand, Result<SalesReturnResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveSalesReturnCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RejectCancellation(
        Guid id,
        DecideSalesReturnRequiredCommentRequest request,
        IValidator<RejectSalesReturnCancellationCommand> validator,
        ICommandHandler<RejectSalesReturnCancellationCommand, Result<SalesReturnResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectSalesReturnCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdateSalesReturnRequest(
    string BranchId,
    Guid WarehouseId,
    Guid CustomerId,
    Guid? DeliveryOrderId,
    DateTimeOffset ReturnDate,
    string? Remarks,
    Guid? CostCenterId,
    string? UpdatedBy,
    List<SalesReturnLineInput> Lines);

// SalesReturnLineInput is defined in CreateSalesReturnCommand.cs and reused as-is for Update — same
// convention as SalesInvoiceLineInput/DeliveryOrderLineInput.

public sealed record SubmitSalesReturnRequest(string RequestedBy);
public sealed record DecideSalesReturnRequest(string ApproverUserId, string? Comments);
public sealed record DecideSalesReturnRequiredCommentRequest(string ApproverUserId, string Comments);
public sealed record CancelSalesReturnRequest(string CancelledBy, string Reason);
public sealed record RequestSalesReturnCancellationRequest(string RequestedBy, string Reason);
