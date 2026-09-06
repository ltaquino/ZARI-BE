using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Items.Create;
using ZARI.Application.Features.Inventory.Items.Delete;
using ZARI.Application.Features.Inventory.Items.Get;
using ZARI.Application.Features.Inventory.Items.GetAll;
using ZARI.Application.Features.Inventory.Items.GetAllPaged;
using ZARI.Application.Features.Inventory.Items.GetByIds;
using ZARI.Application.Features.Inventory.Items.Search;
using ZARI.Application.Features.Inventory.Items.TileMenu;
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

        group.MapGet("/paged", GetAllPaged)
            .WithName("GetAllItemsPaged")
            .WithSummary("Get a page of items, optionally filtered by search text — what the admin Items list page uses");

        group.MapGet("/search", Search)
            .WithName("SearchItems")
            .WithSummary("Search items by code/name, capped at a small result count — what every item picker (ItemPicker, PosScanInput, MultiItemPicker) uses");

        group.MapGet("/by-ids", GetByIds)
            .WithName("GetItemsByIds")
            .WithSummary("Resolve a specific, caller-provided set of item ids to full rows — for screens that only need to display a document's own already-referenced items");

        group.MapGet("/tile-menu", GetTileMenu)
            .WithName("GetTileMenuItems")
            .WithSummary("Items tagged for POS Mode's tile menu, sellable at the given branch");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetItemById")
            .WithSummary("Get an item by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateItemCommand>>()
            .WithName("CreateItem")
            .WithSummary("Create a new item");

        group.MapPut("/{id:guid}", Update)
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

    private static async Task<IResult> GetAllPaged(
        int? page,
        int? pageSize,
        string? search,
        IQueryHandler<GetAllItemsPagedQuery, Result<PagedResult<ItemResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllItemsPagedQuery(page ?? 1, pageSize ?? 20, search), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Search(
        string? q,
        int? limit,
        IQueryHandler<SearchItemsQuery, Result<List<ItemResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new SearchItemsQuery(q, limit ?? 8), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetByIds(
        string? ids,
        IQueryHandler<GetItemsByIdsQuery, Result<List<ItemResponse>>> handler,
        CancellationToken cancellationToken)
    {
        // Deliberately a plain comma-separated string, not ASP.NET's repeated-key array binding
        // (?ids=a&ids=b) — simpler for every FE caller to build and impossible to get wrong.
        var parsedIds = (ids ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Guid.TryParse(s, out var id) ? id : (Guid?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        var result = await handler.HandleAsync(new GetItemsByIdsQuery(parsedIds), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetTileMenu(
        string branchId,
        IQueryHandler<GetTileMenuItemsQuery, Result<List<TileMenuItemResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetTileMenuItemsQuery(branchId), cancellationToken);
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
        IValidator<UpdateItemCommand> validator,
        ICommandHandler<UpdateItemCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateItemCommand(
            id, request.Code, request.Name, request.Description, request.CategoryId, request.BaseUomId, request.ItemType, request.CostingMethod,
            request.IsSerialized, request.IsBatchTracked, request.IsSold, request.IsPurchased, request.IsStocked, request.IsTileDisplay,
            request.SalesAccountId, request.PurchaseAccountId, request.InventoryAccountId, request.CogsAccountId, request.VatType, request.Status);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

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
    bool IsTileDisplay,
    string? SalesAccountId,
    string? PurchaseAccountId,
    string? InventoryAccountId,
    string? CogsAccountId,
    string VatType,
    string Status);
