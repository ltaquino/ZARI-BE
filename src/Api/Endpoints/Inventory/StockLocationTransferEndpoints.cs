using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockLocationTransfers.Cancel;
using ZARI.Application.Features.Inventory.StockLocationTransfers.Create;
using ZARI.Application.Features.Inventory.StockLocationTransfers.Delete;
using ZARI.Application.Features.Inventory.StockLocationTransfers.Get;
using ZARI.Application.Features.Inventory.StockLocationTransfers.GetAll;
using ZARI.Application.Features.Inventory.StockLocationTransfers.GetAllPaged;
using ZARI.Application.Features.Inventory.StockLocationTransfers.Post;
using ZARI.Application.Features.Inventory.StockLocationTransfers.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class StockLocationTransferEndpoints
{
    public static void MapStockLocationTransferEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stock-location-transfers")
            .WithTags("StockLocationTransfers")
            .WithGroupName("Inventory")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllStockLocationTransfers")
            .WithSummary("Get all bin transfers");

        group.MapGet("/paged", GetAllPaged)
            .WithName("GetAllStockLocationTransfersPaged")
            .WithSummary("Get a page of bin transfers, optionally filtered by search text");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetStockLocationTransferById")
            .WithSummary("Get a bin transfer by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateStockLocationTransferCommand>>()
            .WithName("CreateStockLocationTransfer")
            .WithSummary("Create a draft bin transfer");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateStockLocationTransfer")
            .WithSummary("Update a draft bin transfer");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteStockLocationTransfer")
            .WithSummary("Delete a draft bin transfer");

        group.MapPost("/{id:guid}/post", Post)
            .WithName("PostStockLocationTransfer")
            .WithSummary("Post a draft bin transfer directly — no approval step; moves qty between bins");

        group.MapPost("/{id:guid}/cancel", Cancel)
            .WithName("CancelStockLocationTransfer")
            .WithSummary("Cancel a draft bin transfer directly");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllStockLocationTransfersQuery, Result<List<StockLocationTransferResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllStockLocationTransfersQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetAllPaged(
        int? page,
        int? pageSize,
        string? search,
        IQueryHandler<GetAllStockLocationTransfersPagedQuery, Result<PagedResult<StockLocationTransferResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllStockLocationTransfersPagedQuery(page ?? 1, pageSize ?? 20, search), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetStockLocationTransferQuery, Result<StockLocationTransferResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetStockLocationTransferQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateStockLocationTransferCommand command,
        ICommandHandler<CreateStockLocationTransferCommand, Result<StockLocationTransferResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetStockLocationTransferById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateStockLocationTransferRequest request,
        IValidator<UpdateStockLocationTransferCommand> validator,
        ICommandHandler<UpdateStockLocationTransferCommand, Result<StockLocationTransferResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateStockLocationTransferCommand(id, request.BranchId, request.WarehouseId, request.TransferDate, request.Remarks, request.UpdatedBy, request.Lines);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteStockLocationTransferCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteStockLocationTransferCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Post(
        Guid id,
        PostStockLocationTransferRequest request,
        IValidator<PostStockLocationTransferCommand> validator,
        ICommandHandler<PostStockLocationTransferCommand, Result<StockLocationTransferResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new PostStockLocationTransferCommand(id, request.PostedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Cancel(
        Guid id,
        CancelStockLocationTransferRequest request,
        IValidator<CancelStockLocationTransferCommand> validator,
        ICommandHandler<CancelStockLocationTransferCommand, Result<StockLocationTransferResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CancelStockLocationTransferCommand(id, request.CancelledBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdateStockLocationTransferRequest(
    string BranchId,
    Guid WarehouseId,
    DateTimeOffset TransferDate,
    string? Remarks,
    string? UpdatedBy,
    List<StockLocationTransferLineInput> Lines);

public sealed record PostStockLocationTransferRequest(string PostedBy);
public sealed record CancelStockLocationTransferRequest(string CancelledBy, string Reason);
