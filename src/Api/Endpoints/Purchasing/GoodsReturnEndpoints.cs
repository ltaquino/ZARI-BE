using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReturns.ApproveCancellation;
using ZARI.Application.Features.Purchasing.GoodsReturns.Approve;
using ZARI.Application.Features.Purchasing.GoodsReturns.Cancel;
using ZARI.Application.Features.Purchasing.GoodsReturns.Create;
using ZARI.Application.Features.Purchasing.GoodsReturns.Delete;
using ZARI.Application.Features.Purchasing.GoodsReturns.Get;
using ZARI.Application.Features.Purchasing.GoodsReturns.GetAll;
using ZARI.Application.Features.Purchasing.GoodsReturns.Reject;
using ZARI.Application.Features.Purchasing.GoodsReturns.RejectCancellation;
using ZARI.Application.Features.Purchasing.GoodsReturns.RequestCancellation;
using ZARI.Application.Features.Purchasing.GoodsReturns.Submit;
using ZARI.Application.Features.Purchasing.GoodsReturns.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class GoodsReturnEndpoints
{
    public static void MapGoodsReturnEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/goods-returns")
            .WithTags("GoodsReturns")
            .WithGroupName("Purchasing")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllGoodsReturns")
            .WithSummary("Get all goods returns");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetGoodsReturnById")
            .WithSummary("Get a goods return by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateGoodsReturnCommand>>()
            .WithName("CreateGoodsReturn")
            .WithSummary("Create a draft goods return");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateGoodsReturn")
            .WithSummary("Update a draft goods return");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteGoodsReturn")
            .WithSummary("Delete a draft goods return");

        group.MapPost("/{id:guid}/submit", Submit)
            .WithName("SubmitGoodsReturn")
            .WithSummary("Submit a draft goods return for approval");

        group.MapPost("/{id:guid}/approve", Approve)
            .WithName("ApproveGoodsReturn")
            .WithSummary("Approve a pending goods return — issues stock and posts the GRNI reversal GL journal");

        group.MapPost("/{id:guid}/reject", Reject)
            .WithName("RejectGoodsReturn")
            .WithSummary("Reject a pending goods return back to draft");

        group.MapPost("/{id:guid}/cancel", Cancel)
            .WithName("CancelGoodsReturn")
            .WithSummary("Cancel a draft or pending-approval goods return directly");

        group.MapPost("/{id:guid}/request-cancellation", RequestCancellation)
            .WithName("RequestGoodsReturnCancellation")
            .WithSummary("Request cancellation of a posted goods return");

        group.MapPost("/{id:guid}/approve-cancellation", ApproveCancellation)
            .WithName("ApproveGoodsReturnCancellation")
            .WithSummary("Approve a cancellation request — reverses stock, serials, and the GL journal");

        group.MapPost("/{id:guid}/reject-cancellation", RejectCancellation)
            .WithName("RejectGoodsReturnCancellation")
            .WithSummary("Reject a cancellation request — the document stands as posted");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllGoodsReturnsQuery, Result<List<GoodsReturnResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllGoodsReturnsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetGoodsReturnQuery, Result<GoodsReturnResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetGoodsReturnQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateGoodsReturnCommand command,
        ICommandHandler<CreateGoodsReturnCommand, Result<GoodsReturnResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetGoodsReturnById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateGoodsReturnRequest request,
        IValidator<UpdateGoodsReturnCommand> validator,
        ICommandHandler<UpdateGoodsReturnCommand, Result<GoodsReturnResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateGoodsReturnCommand(
            id, request.BranchId, request.WarehouseId, request.SupplierId, request.GoodsReceiptPoId,
            request.ReasonCode, request.ReturnDate, request.Remarks, request.CostCenterId, request.UpdatedBy, request.Lines);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteGoodsReturnCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteGoodsReturnCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Submit(
        Guid id,
        SubmitGoodsReturnRequest request,
        IValidator<SubmitGoodsReturnCommand> validator,
        ICommandHandler<SubmitGoodsReturnCommand, Result<GoodsReturnResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new SubmitGoodsReturnCommand(id, request.RequestedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Approve(
        Guid id,
        DecideGoodsReturnRequest request,
        IValidator<ApproveGoodsReturnCommand> validator,
        ICommandHandler<ApproveGoodsReturnCommand, Result<GoodsReturnResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveGoodsReturnCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Reject(
        Guid id,
        DecideGoodsReturnRequiredCommentRequest request,
        IValidator<RejectGoodsReturnCommand> validator,
        ICommandHandler<RejectGoodsReturnCommand, Result<GoodsReturnResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectGoodsReturnCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Cancel(
        Guid id,
        CancelGoodsReturnRequest request,
        IValidator<CancelGoodsReturnCommand> validator,
        ICommandHandler<CancelGoodsReturnCommand, Result<GoodsReturnResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CancelGoodsReturnCommand(id, request.CancelledBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RequestCancellation(
        Guid id,
        RequestGoodsReturnCancellationRequest request,
        IValidator<RequestGoodsReturnCancellationCommand> validator,
        ICommandHandler<RequestGoodsReturnCancellationCommand, Result<GoodsReturnResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RequestGoodsReturnCancellationCommand(id, request.RequestedBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> ApproveCancellation(
        Guid id,
        DecideGoodsReturnRequest request,
        IValidator<ApproveGoodsReturnCancellationCommand> validator,
        ICommandHandler<ApproveGoodsReturnCancellationCommand, Result<GoodsReturnResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveGoodsReturnCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RejectCancellation(
        Guid id,
        DecideGoodsReturnRequiredCommentRequest request,
        IValidator<RejectGoodsReturnCancellationCommand> validator,
        ICommandHandler<RejectGoodsReturnCancellationCommand, Result<GoodsReturnResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectGoodsReturnCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdateGoodsReturnRequest(
    string BranchId,
    Guid WarehouseId,
    Guid SupplierId,
    Guid? GoodsReceiptPoId,
    string ReasonCode,
    DateTimeOffset ReturnDate,
    string? Remarks,
    Guid? CostCenterId,
    string? UpdatedBy,
    List<GoodsReturnLineInput> Lines);

public sealed record SubmitGoodsReturnRequest(string RequestedBy);
public sealed record DecideGoodsReturnRequest(string ApproverUserId, string? Comments);
public sealed record DecideGoodsReturnRequiredCommentRequest(string ApproverUserId, string Comments);
public sealed record CancelGoodsReturnRequest(string CancelledBy, string Reason);
public sealed record RequestGoodsReturnCancellationRequest(string RequestedBy, string Reason);
