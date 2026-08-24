using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockTransferRequests.Approve;
using ZARI.Application.Features.Inventory.StockTransferRequests.Cancel;
using ZARI.Application.Features.Inventory.StockTransferRequests.Create;
using ZARI.Application.Features.Inventory.StockTransferRequests.Decline;
using ZARI.Application.Features.Inventory.StockTransferRequests.Delete;
using ZARI.Application.Features.Inventory.StockTransferRequests.Get;
using ZARI.Application.Features.Inventory.StockTransferRequests.GetAll;
using ZARI.Application.Features.Inventory.StockTransferRequests.Reject;
using ZARI.Application.Features.Inventory.StockTransferRequests.Submit;
using ZARI.Application.Features.Inventory.StockTransferRequests.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class StockTransferRequestEndpoints
{
    public static void MapStockTransferRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stock-transfer-requests")
            .WithTags("StockTransferRequests")
            .WithGroupName("Inventory")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllStockTransferRequests")
            .WithSummary("Get all stock transfer requests");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetStockTransferRequestById")
            .WithSummary("Get a stock transfer request by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateStockTransferRequestCommand>>()
            .WithName("CreateStockTransferRequest")
            .WithSummary("Create a draft stock transfer request");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateStockTransferRequest")
            .WithSummary("Update a draft stock transfer request");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteStockTransferRequest")
            .WithSummary("Delete a draft stock transfer request");

        group.MapPost("/{id:guid}/submit", Submit)
            .WithName("SubmitStockTransferRequest")
            .WithSummary("Submit a draft stock transfer request for approval");

        group.MapPost("/{id:guid}/approve", Approve)
            .WithName("ApproveStockTransferRequest")
            .WithSummary("Approve a pending stock transfer request — decided by the requesting branch's own manager");

        group.MapPost("/{id:guid}/reject", Reject)
            .WithName("RejectStockTransferRequest")
            .WithSummary("Reject a pending stock transfer request back to draft");

        group.MapPost("/{id:guid}/decline", Decline)
            .WithName("DeclineStockTransferRequest")
            .WithSummary("Decline an approved request — decided by the fulfilling (source) branch's manager");

        group.MapPost("/{id:guid}/cancel", Cancel)
            .WithName("CancelStockTransferRequest")
            .WithSummary("Cancel/withdraw a draft, pending, or approved stock transfer request");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllStockTransferRequestsQuery, Result<List<StockTransferRequestResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllStockTransferRequestsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetStockTransferRequestQuery, Result<StockTransferRequestResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetStockTransferRequestQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateStockTransferRequestCommand command,
        ICommandHandler<CreateStockTransferRequestCommand, Result<StockTransferRequestResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetStockTransferRequestById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateStockTransferRequestRequest request,
        IValidator<UpdateStockTransferRequestCommand> validator,
        ICommandHandler<UpdateStockTransferRequestCommand, Result<StockTransferRequestResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateStockTransferRequestCommand(
            id, request.SourceBranchId, request.SourceWarehouseId, request.DestBranchId, request.DestWarehouseId,
            request.RequestDate, request.Remarks, request.UpdatedBy, request.Lines);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteStockTransferRequestCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteStockTransferRequestCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Submit(
        Guid id,
        SubmitStockTransferRequestRequest request,
        IValidator<SubmitStockTransferRequestCommand> validator,
        ICommandHandler<SubmitStockTransferRequestCommand, Result<StockTransferRequestResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new SubmitStockTransferRequestCommand(id, request.RequestedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Approve(
        Guid id,
        DecideStockTransferRequestRequest request,
        IValidator<ApproveStockTransferRequestCommand> validator,
        ICommandHandler<ApproveStockTransferRequestCommand, Result<StockTransferRequestResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveStockTransferRequestCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Reject(
        Guid id,
        DecideStockTransferRequestRequiredCommentRequest request,
        IValidator<RejectStockTransferRequestCommand> validator,
        ICommandHandler<RejectStockTransferRequestCommand, Result<StockTransferRequestResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectStockTransferRequestCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Decline(
        Guid id,
        DeclineStockTransferRequestRequest request,
        IValidator<DeclineStockTransferRequestCommand> validator,
        ICommandHandler<DeclineStockTransferRequestCommand, Result<StockTransferRequestResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new DeclineStockTransferRequestCommand(id, request.DeclinedBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Cancel(
        Guid id,
        CancelStockTransferRequestRequest request,
        IValidator<CancelStockTransferRequestCommand> validator,
        ICommandHandler<CancelStockTransferRequestCommand, Result<StockTransferRequestResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CancelStockTransferRequestCommand(id, request.CancelledBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdateStockTransferRequestRequest(
    string SourceBranchId,
    Guid SourceWarehouseId,
    string DestBranchId,
    Guid DestWarehouseId,
    DateTimeOffset RequestDate,
    string? Remarks,
    string? UpdatedBy,
    List<StockTransferRequestLineInput> Lines);

public sealed record SubmitStockTransferRequestRequest(string RequestedBy);
public sealed record DecideStockTransferRequestRequest(string ApproverUserId, string? Comments);
public sealed record DecideStockTransferRequestRequiredCommentRequest(string ApproverUserId, string Comments);
public sealed record DeclineStockTransferRequestRequest(string DeclinedBy, string Reason);
public sealed record CancelStockTransferRequestRequest(string CancelledBy, string Reason);
