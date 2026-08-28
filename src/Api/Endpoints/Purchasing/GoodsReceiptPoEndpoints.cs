using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.ApproveCancellation;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.Approve;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.Cancel;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.Create;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.Delete;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.Get;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.GetAll;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.Reject;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.RejectCancellation;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.RequestCancellation;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.Submit;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class GoodsReceiptPoEndpoints
{
    public static void MapGoodsReceiptPoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/goods-receipt-po")
            .WithTags("GoodsReceiptPo")
            .WithGroupName("Purchasing")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllGoodsReceiptPos")
            .WithSummary("Get all goods receipts (PO)");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetGoodsReceiptPoById")
            .WithSummary("Get a goods receipt (PO) by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateGoodsReceiptPoCommand>>()
            .WithName("CreateGoodsReceiptPo")
            .WithSummary("Create a draft goods receipt (PO)");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateGoodsReceiptPo")
            .WithSummary("Update a draft goods receipt (PO)");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteGoodsReceiptPo")
            .WithSummary("Delete a draft goods receipt (PO)");

        group.MapPost("/{id:guid}/submit", Submit)
            .WithName("SubmitGoodsReceiptPo")
            .WithSummary("Submit a draft goods receipt (PO) for approval");

        group.MapPost("/{id:guid}/approve", Approve)
            .WithName("ApproveGoodsReceiptPo")
            .WithSummary("Approve a pending goods receipt (PO) — posts stock, serials, location balances, and the GRNI GL journal");

        group.MapPost("/{id:guid}/reject", Reject)
            .WithName("RejectGoodsReceiptPo")
            .WithSummary("Reject a pending goods receipt (PO) back to draft");

        group.MapPost("/{id:guid}/cancel", Cancel)
            .WithName("CancelGoodsReceiptPo")
            .WithSummary("Cancel a draft or pending-approval goods receipt (PO) directly");

        group.MapPost("/{id:guid}/request-cancellation", RequestCancellation)
            .WithName("RequestGoodsReceiptPoCancellation")
            .WithSummary("Request cancellation of a posted goods receipt (PO)");

        group.MapPost("/{id:guid}/approve-cancellation", ApproveCancellation)
            .WithName("ApproveGoodsReceiptPoCancellation")
            .WithSummary("Approve a cancellation request — reverses stock, serials, and the GL journal");

        group.MapPost("/{id:guid}/reject-cancellation", RejectCancellation)
            .WithName("RejectGoodsReceiptPoCancellation")
            .WithSummary("Reject a cancellation request — the document stands as posted");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllGoodsReceiptPosQuery, Result<List<GoodsReceiptPoResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllGoodsReceiptPosQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetGoodsReceiptPoQuery, Result<GoodsReceiptPoResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetGoodsReceiptPoQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateGoodsReceiptPoCommand command,
        ICommandHandler<CreateGoodsReceiptPoCommand, Result<GoodsReceiptPoResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetGoodsReceiptPoById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateGoodsReceiptPoRequest request,
        IValidator<UpdateGoodsReceiptPoCommand> validator,
        ICommandHandler<UpdateGoodsReceiptPoCommand, Result<GoodsReceiptPoResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateGoodsReceiptPoCommand(
            id, request.BranchId, request.WarehouseId, request.SupplierId, request.PurchaseOrderId,
            request.SupplierInvoiceNo, request.ReceiptDate, request.Remarks, request.CostCenterId, request.UpdatedBy, request.Lines);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteGoodsReceiptPoCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteGoodsReceiptPoCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Submit(
        Guid id,
        SubmitGoodsReceiptPoRequest request,
        IValidator<SubmitGoodsReceiptPoCommand> validator,
        ICommandHandler<SubmitGoodsReceiptPoCommand, Result<GoodsReceiptPoResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new SubmitGoodsReceiptPoCommand(id, request.RequestedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Approve(
        Guid id,
        DecideGoodsReceiptPoRequest request,
        IValidator<ApproveGoodsReceiptPoCommand> validator,
        ICommandHandler<ApproveGoodsReceiptPoCommand, Result<GoodsReceiptPoResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveGoodsReceiptPoCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Reject(
        Guid id,
        DecideGoodsReceiptPoRequiredCommentRequest request,
        IValidator<RejectGoodsReceiptPoCommand> validator,
        ICommandHandler<RejectGoodsReceiptPoCommand, Result<GoodsReceiptPoResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectGoodsReceiptPoCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Cancel(
        Guid id,
        CancelGoodsReceiptPoRequest request,
        IValidator<CancelGoodsReceiptPoCommand> validator,
        ICommandHandler<CancelGoodsReceiptPoCommand, Result<GoodsReceiptPoResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CancelGoodsReceiptPoCommand(id, request.CancelledBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RequestCancellation(
        Guid id,
        RequestGoodsReceiptPoCancellationRequest request,
        IValidator<RequestGoodsReceiptPoCancellationCommand> validator,
        ICommandHandler<RequestGoodsReceiptPoCancellationCommand, Result<GoodsReceiptPoResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RequestGoodsReceiptPoCancellationCommand(id, request.RequestedBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> ApproveCancellation(
        Guid id,
        DecideGoodsReceiptPoRequest request,
        IValidator<ApproveGoodsReceiptPoCancellationCommand> validator,
        ICommandHandler<ApproveGoodsReceiptPoCancellationCommand, Result<GoodsReceiptPoResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveGoodsReceiptPoCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RejectCancellation(
        Guid id,
        DecideGoodsReceiptPoRequiredCommentRequest request,
        IValidator<RejectGoodsReceiptPoCancellationCommand> validator,
        ICommandHandler<RejectGoodsReceiptPoCancellationCommand, Result<GoodsReceiptPoResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectGoodsReceiptPoCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdateGoodsReceiptPoRequest(
    string BranchId,
    Guid WarehouseId,
    Guid SupplierId,
    Guid? PurchaseOrderId,
    string? SupplierInvoiceNo,
    DateTimeOffset ReceiptDate,
    string? Remarks,
    Guid? CostCenterId,
    string? UpdatedBy,
    List<GoodsReceiptPoLineInput> Lines);

public sealed record SubmitGoodsReceiptPoRequest(string RequestedBy);
public sealed record DecideGoodsReceiptPoRequest(string ApproverUserId, string? Comments);
public sealed record DecideGoodsReceiptPoRequiredCommentRequest(string ApproverUserId, string Comments);
public sealed record CancelGoodsReceiptPoRequest(string CancelledBy, string Reason);
public sealed record RequestGoodsReceiptPoCancellationRequest(string RequestedBy, string Reason);
