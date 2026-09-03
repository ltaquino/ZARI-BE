using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseRequests.Approve;
using ZARI.Application.Features.Purchasing.PurchaseRequests.Cancel;
using ZARI.Application.Features.Purchasing.PurchaseRequests.Create;
using ZARI.Application.Features.Purchasing.PurchaseRequests.Delete;
using ZARI.Application.Features.Purchasing.PurchaseRequests.Get;
using ZARI.Application.Features.Purchasing.PurchaseRequests.GetAll;
using ZARI.Application.Features.Purchasing.PurchaseRequests.GetAllPaged;
using ZARI.Application.Features.Purchasing.PurchaseRequests.Reject;
using ZARI.Application.Features.Purchasing.PurchaseRequests.Submit;
using ZARI.Application.Features.Purchasing.PurchaseRequests.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class PurchaseRequestEndpoints
{
    public static void MapPurchaseRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/purchase-requests")
            .WithTags("PurchaseRequests")
            .WithGroupName("Purchasing")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllPurchaseRequests")
            .WithSummary("Get all purchase requests");

        group.MapGet("/paged", GetAllPaged)
            .WithName("GetAllPurchaseRequestsPaged")
            .WithSummary("Get a page of purchase requests, optionally filtered by search text");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetPurchaseRequestById")
            .WithSummary("Get a purchase request by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreatePurchaseRequestCommand>>()
            .WithName("CreatePurchaseRequest")
            .WithSummary("Create a draft purchase request");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdatePurchaseRequest")
            .WithSummary("Update a draft purchase request");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeletePurchaseRequest")
            .WithSummary("Delete a draft purchase request");

        group.MapPost("/{id:guid}/submit", Submit)
            .WithName("SubmitPurchaseRequest")
            .WithSummary("Submit a draft purchase request for approval");

        group.MapPost("/{id:guid}/approve", Approve)
            .WithName("ApprovePurchaseRequest")
            .WithSummary("Approve a pending purchase request");

        group.MapPost("/{id:guid}/reject", Reject)
            .WithName("RejectPurchaseRequest")
            .WithSummary("Reject a pending purchase request back to draft");

        group.MapPost("/{id:guid}/cancel", Cancel)
            .WithName("CancelPurchaseRequest")
            .WithSummary("Cancel a purchase request that has not yet been superseded by a purchase order");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllPurchaseRequestsQuery, Result<List<PurchaseRequestResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllPurchaseRequestsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetAllPaged(
        int? page,
        int? pageSize,
        string? search,
        IQueryHandler<GetAllPurchaseRequestsPagedQuery, Result<PagedResult<PurchaseRequestResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllPurchaseRequestsPagedQuery(page ?? 1, pageSize ?? 20, search), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetPurchaseRequestQuery, Result<PurchaseRequestResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetPurchaseRequestQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreatePurchaseRequestCommand command,
        ICommandHandler<CreatePurchaseRequestCommand, Result<PurchaseRequestResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetPurchaseRequestById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdatePurchaseRequestRequest request,
        IValidator<UpdatePurchaseRequestCommand> validator,
        ICommandHandler<UpdatePurchaseRequestCommand, Result<PurchaseRequestResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePurchaseRequestCommand(
            id, request.BranchId, request.RequestDate, request.Remarks, request.UpdatedBy, request.Lines);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeletePurchaseRequestCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeletePurchaseRequestCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Submit(
        Guid id,
        SubmitPurchaseRequestRequest request,
        IValidator<SubmitPurchaseRequestCommand> validator,
        ICommandHandler<SubmitPurchaseRequestCommand, Result<PurchaseRequestResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new SubmitPurchaseRequestCommand(id, request.RequestedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Approve(
        Guid id,
        DecidePurchaseRequestRequest request,
        IValidator<ApprovePurchaseRequestCommand> validator,
        ICommandHandler<ApprovePurchaseRequestCommand, Result<PurchaseRequestResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApprovePurchaseRequestCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Reject(
        Guid id,
        DecidePurchaseRequestRequiredCommentRequest request,
        IValidator<RejectPurchaseRequestCommand> validator,
        ICommandHandler<RejectPurchaseRequestCommand, Result<PurchaseRequestResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectPurchaseRequestCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Cancel(
        Guid id,
        CancelPurchaseRequestRequest request,
        IValidator<CancelPurchaseRequestCommand> validator,
        ICommandHandler<CancelPurchaseRequestCommand, Result<PurchaseRequestResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CancelPurchaseRequestCommand(id, request.CancelledBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdatePurchaseRequestRequest(
    string BranchId,
    DateTimeOffset RequestDate,
    string? Remarks,
    string? UpdatedBy,
    List<PurchaseRequestLineInput> Lines);

public sealed record SubmitPurchaseRequestRequest(string RequestedBy);
public sealed record DecidePurchaseRequestRequest(string ApproverUserId, string? Comments);
public sealed record DecidePurchaseRequestRequiredCommentRequest(string ApproverUserId, string Comments);
public sealed record CancelPurchaseRequestRequest(string CancelledBy, string Reason);
