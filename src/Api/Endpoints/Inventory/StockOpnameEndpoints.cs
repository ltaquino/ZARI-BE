using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockOpnames.ApproveCancellation;
using ZARI.Application.Features.Inventory.StockOpnames.Cancel;
using ZARI.Application.Features.Inventory.StockOpnames.Create;
using ZARI.Application.Features.Inventory.StockOpnames.Delete;
using ZARI.Application.Features.Inventory.StockOpnames.Get;
using ZARI.Application.Features.Inventory.StockOpnames.GetAll;
using ZARI.Application.Features.Inventory.StockOpnames.Post;
using ZARI.Application.Features.Inventory.StockOpnames.RejectCancellation;
using ZARI.Application.Features.Inventory.StockOpnames.RequestCancellation;
using ZARI.Application.Features.Inventory.StockOpnames.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class StockOpnameEndpoints
{
    public static void MapStockOpnameEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stock-opnames")
            .WithTags("StockOpnames")
            .WithGroupName("Inventory")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllStockOpnames")
            .WithSummary("Get all stock counts");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetStockOpnameById")
            .WithSummary("Get a stock count by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateStockOpnameCommand>>()
            .WithName("CreateStockOpname")
            .WithSummary("Create a draft stock count");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateStockOpname")
            .WithSummary("Update a draft stock count");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteStockOpname")
            .WithSummary("Delete a draft stock count");

        group.MapPost("/{id:guid}/post", Post)
            .WithName("PostStockOpname")
            .WithSummary("Post a draft stock count directly — no approval step; posts stock, serials, and the GL journal");

        group.MapPost("/{id:guid}/cancel", Cancel)
            .WithName("CancelStockOpname")
            .WithSummary("Cancel a draft stock count directly");

        group.MapPost("/{id:guid}/request-cancellation", RequestCancellation)
            .WithName("RequestStockOpnameCancellation")
            .WithSummary("Request cancellation of a posted stock count");

        group.MapPost("/{id:guid}/approve-cancellation", ApproveCancellation)
            .WithName("ApproveStockOpnameCancellation")
            .WithSummary("Approve a cancellation request — reverses stock, serials, and the GL journal");

        group.MapPost("/{id:guid}/reject-cancellation", RejectCancellation)
            .WithName("RejectStockOpnameCancellation")
            .WithSummary("Reject a cancellation request — the document stands as posted");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllStockOpnamesQuery, Result<List<StockOpnameResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllStockOpnamesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetStockOpnameQuery, Result<StockOpnameResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetStockOpnameQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateStockOpnameCommand command,
        ICommandHandler<CreateStockOpnameCommand, Result<StockOpnameResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetStockOpnameById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateStockOpnameRequest request,
        IValidator<UpdateStockOpnameCommand> validator,
        ICommandHandler<UpdateStockOpnameCommand, Result<StockOpnameResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateStockOpnameCommand(id, request.BranchId, request.WarehouseId, request.CountDate, request.Remarks, request.CostCenterId, request.UpdatedBy, request.Lines);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteStockOpnameCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteStockOpnameCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Post(
        Guid id,
        PostStockOpnameRequest request,
        IValidator<PostStockOpnameCommand> validator,
        ICommandHandler<PostStockOpnameCommand, Result<StockOpnameResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new PostStockOpnameCommand(id, request.PostedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Cancel(
        Guid id,
        CancelStockOpnameRequest request,
        IValidator<CancelStockOpnameCommand> validator,
        ICommandHandler<CancelStockOpnameCommand, Result<StockOpnameResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CancelStockOpnameCommand(id, request.CancelledBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RequestCancellation(
        Guid id,
        RequestStockOpnameCancellationRequest request,
        IValidator<RequestStockOpnameCancellationCommand> validator,
        ICommandHandler<RequestStockOpnameCancellationCommand, Result<StockOpnameResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RequestStockOpnameCancellationCommand(id, request.RequestedBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> ApproveCancellation(
        Guid id,
        DecideStockOpnameRequest request,
        IValidator<ApproveStockOpnameCancellationCommand> validator,
        ICommandHandler<ApproveStockOpnameCancellationCommand, Result<StockOpnameResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveStockOpnameCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RejectCancellation(
        Guid id,
        DecideStockOpnameRequiredCommentRequest request,
        IValidator<RejectStockOpnameCancellationCommand> validator,
        ICommandHandler<RejectStockOpnameCancellationCommand, Result<StockOpnameResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectStockOpnameCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdateStockOpnameRequest(
    string BranchId,
    Guid WarehouseId,
    DateTimeOffset CountDate,
    string? Remarks,
    Guid? CostCenterId,
    string? UpdatedBy,
    List<StockOpnameLineInput> Lines);

public sealed record PostStockOpnameRequest(string PostedBy);
public sealed record DecideStockOpnameRequest(string ApproverUserId, string? Comments);
public sealed record DecideStockOpnameRequiredCommentRequest(string ApproverUserId, string Comments);
public sealed record CancelStockOpnameRequest(string CancelledBy, string Reason);
public sealed record RequestStockOpnameCancellationRequest(string RequestedBy, string Reason);
