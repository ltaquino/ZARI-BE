using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsReceipts.ApproveCancellation;
using ZARI.Application.Features.Inventory.GoodsReceipts.Approve;
using ZARI.Application.Features.Inventory.GoodsReceipts.Cancel;
using ZARI.Application.Features.Inventory.GoodsReceipts.Create;
using ZARI.Application.Features.Inventory.GoodsReceipts.Delete;
using ZARI.Application.Features.Inventory.GoodsReceipts.Get;
using ZARI.Application.Features.Inventory.GoodsReceipts.GetAll;
using ZARI.Application.Features.Inventory.GoodsReceipts.Reject;
using ZARI.Application.Features.Inventory.GoodsReceipts.RejectCancellation;
using ZARI.Application.Features.Inventory.GoodsReceipts.RequestCancellation;
using ZARI.Application.Features.Inventory.GoodsReceipts.Submit;
using ZARI.Application.Features.Inventory.GoodsReceipts.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class GoodsReceiptEndpoints
{
    public static void MapGoodsReceiptEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/goods-receipts")
            .WithTags("GoodsReceipts")
            .WithGroupName("Inventory")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllGoodsReceipts")
            .WithSummary("Get all goods receipts");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetGoodsReceiptById")
            .WithSummary("Get a goods receipt by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateGoodsReceiptCommand>>()
            .WithName("CreateGoodsReceipt")
            .WithSummary("Create a draft goods receipt");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateGoodsReceipt")
            .WithSummary("Update a draft goods receipt");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteGoodsReceipt")
            .WithSummary("Delete a draft goods receipt");

        group.MapPost("/{id:guid}/submit", Submit)
            .WithName("SubmitGoodsReceipt")
            .WithSummary("Submit a draft goods receipt for approval");

        group.MapPost("/{id:guid}/approve", Approve)
            .WithName("ApproveGoodsReceipt")
            .WithSummary("Approve a pending goods receipt — posts stock, serials, location balances, and the GL journal");

        group.MapPost("/{id:guid}/reject", Reject)
            .WithName("RejectGoodsReceipt")
            .WithSummary("Reject a pending goods receipt back to draft");

        group.MapPost("/{id:guid}/cancel", Cancel)
            .WithName("CancelGoodsReceipt")
            .WithSummary("Cancel a draft or pending-approval goods receipt directly");

        group.MapPost("/{id:guid}/request-cancellation", RequestCancellation)
            .WithName("RequestGoodsReceiptCancellation")
            .WithSummary("Request cancellation of a posted goods receipt");

        group.MapPost("/{id:guid}/approve-cancellation", ApproveCancellation)
            .WithName("ApproveGoodsReceiptCancellation")
            .WithSummary("Approve a cancellation request — reverses stock, serials, and the GL journal");

        group.MapPost("/{id:guid}/reject-cancellation", RejectCancellation)
            .WithName("RejectGoodsReceiptCancellation")
            .WithSummary("Reject a cancellation request — the document stands as posted");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllGoodsReceiptsQuery, Result<List<GoodsReceiptResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllGoodsReceiptsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetGoodsReceiptQuery, Result<GoodsReceiptResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetGoodsReceiptQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateGoodsReceiptCommand command,
        ICommandHandler<CreateGoodsReceiptCommand, Result<GoodsReceiptResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetGoodsReceiptById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateGoodsReceiptRequest request,
        IValidator<UpdateGoodsReceiptCommand> validator,
        ICommandHandler<UpdateGoodsReceiptCommand, Result<GoodsReceiptResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateGoodsReceiptCommand(
            id, request.BranchId, request.WarehouseId, request.ReceiptType, request.ReceivedBy, request.GrDate, request.Remarks,
            request.GoodsIssueRefNo, request.GoodsIssueId, request.ReasonCode, request.UpdatedBy, request.Lines);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteGoodsReceiptCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteGoodsReceiptCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Submit(
        Guid id,
        SubmitGoodsReceiptRequest request,
        IValidator<SubmitGoodsReceiptCommand> validator,
        ICommandHandler<SubmitGoodsReceiptCommand, Result<GoodsReceiptResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new SubmitGoodsReceiptCommand(id, request.RequestedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Approve(
        Guid id,
        DecideGoodsReceiptRequest request,
        IValidator<ApproveGoodsReceiptCommand> validator,
        ICommandHandler<ApproveGoodsReceiptCommand, Result<GoodsReceiptResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveGoodsReceiptCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Reject(
        Guid id,
        DecideGoodsReceiptRequiredCommentRequest request,
        IValidator<RejectGoodsReceiptCommand> validator,
        ICommandHandler<RejectGoodsReceiptCommand, Result<GoodsReceiptResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectGoodsReceiptCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Cancel(
        Guid id,
        CancelGoodsReceiptRequest request,
        IValidator<CancelGoodsReceiptCommand> validator,
        ICommandHandler<CancelGoodsReceiptCommand, Result<GoodsReceiptResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CancelGoodsReceiptCommand(id, request.CancelledBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RequestCancellation(
        Guid id,
        RequestGoodsReceiptCancellationRequest request,
        IValidator<RequestGoodsReceiptCancellationCommand> validator,
        ICommandHandler<RequestGoodsReceiptCancellationCommand, Result<GoodsReceiptResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RequestGoodsReceiptCancellationCommand(id, request.RequestedBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> ApproveCancellation(
        Guid id,
        DecideGoodsReceiptRequest request,
        IValidator<ApproveGoodsReceiptCancellationCommand> validator,
        ICommandHandler<ApproveGoodsReceiptCancellationCommand, Result<GoodsReceiptResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveGoodsReceiptCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RejectCancellation(
        Guid id,
        DecideGoodsReceiptRequiredCommentRequest request,
        IValidator<RejectGoodsReceiptCancellationCommand> validator,
        ICommandHandler<RejectGoodsReceiptCancellationCommand, Result<GoodsReceiptResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectGoodsReceiptCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdateGoodsReceiptRequest(
    string BranchId,
    Guid WarehouseId,
    string ReceiptType,
    string? ReceivedBy,
    DateTimeOffset GrDate,
    string? Remarks,
    string? GoodsIssueRefNo,
    string? GoodsIssueId,
    string? ReasonCode,
    string? UpdatedBy,
    List<GoodsReceiptLineInput> Lines);

public sealed record SubmitGoodsReceiptRequest(string RequestedBy);
public sealed record DecideGoodsReceiptRequest(string ApproverUserId, string? Comments);
public sealed record DecideGoodsReceiptRequiredCommentRequest(string ApproverUserId, string Comments);
public sealed record CancelGoodsReceiptRequest(string CancelledBy, string Reason);
public sealed record RequestGoodsReceiptCancellationRequest(string RequestedBy, string Reason);
