using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Items.Create;
using ZARI.Application.Features.Inventory.Items.Delete;
using ZARI.Application.Features.Inventory.Items.Get;
using ZARI.Application.Features.Inventory.Items.GetAll;
using ZARI.Application.Features.Inventory.Items.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class ItemEndpoints
{
    public static void MapItemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/items")
            .WithTags("Items")
            .WithGroupName("Inventory")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllItems")
            .WithSummary("Get all items");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetItemById")
            .WithSummary("Get an item by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateItemCommand>>()
            .WithName("CreateItem")
            .WithSummary("Create a new item");

        group.MapPut("/{id:guid}", Update)
            .AddEndpointFilter<ValidationFilter<UpdateItemCommand>>()
            .WithName("UpdateItem")
            .WithSummary("Update an existing item");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteItem")
            .WithSummary("Delete an item");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllItemsQuery, Result<List<ItemResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllItemsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetItemQuery, Result<ItemResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetItemQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateItemCommand command,
        ICommandHandler<CreateItemCommand, Result<ItemResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetItemById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateItemRequest request,
        ICommandHandler<UpdateItemCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateItemCommand(
            id, request.Code, request.Name, request.Description, request.CategoryId, request.BaseUomId, request.ItemType, request.CostingMethod,
            request.IsSerialized, request.IsBatchTracked, request.IsSold, request.IsPurchased, request.IsStocked,
            request.SalesAccountId, request.PurchaseAccountId, request.InventoryAccountId, request.CogsAccountId, request.Status);

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteItemCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteItemCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateItemRequest(
    string Code,
    string Name,
    string? Description,
    Guid? CategoryId,
    Guid BaseUomId,
    string ItemType,
    string CostingMethod,
    bool IsSerialized,
    bool IsBatchTracked,
    bool IsSold,
    bool IsPurchased,
    bool IsStocked,
    string? SalesAccountId,
    string? PurchaseAccountId,
    string? InventoryAccountId,
    string? CogsAccountId,
    string Status);
