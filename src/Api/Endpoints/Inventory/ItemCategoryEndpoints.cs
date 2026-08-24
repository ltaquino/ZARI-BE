using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.ItemCategories.Create;
using ZARI.Application.Features.Inventory.ItemCategories.Delete;
using ZARI.Application.Features.Inventory.ItemCategories.Get;
using ZARI.Application.Features.Inventory.ItemCategories.GetAll;
using ZARI.Application.Features.Inventory.ItemCategories.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class ItemCategoryEndpoints
{
    public static void MapItemCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/item-categories")
            .WithTags("ItemCategories")
            .WithGroupName("Inventory")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllItemCategories")
            .WithSummary("Get all item categories");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetItemCategoryById")
            .WithSummary("Get an item category by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateItemCategoryCommand>>()
            .WithName("CreateItemCategory")
            .WithSummary("Create a new item category");

        group.MapPut("/{id:guid}", Update)
            .AddEndpointFilter<ValidationFilter<UpdateItemCategoryCommand>>()
            .WithName("UpdateItemCategory")
            .WithSummary("Update an existing item category");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteItemCategory")
            .WithSummary("Delete an item category");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllItemCategoriesQuery, Result<List<ItemCategoryResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllItemCategoriesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetItemCategoryQuery, Result<ItemCategoryResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetItemCategoryQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateItemCategoryCommand command,
        ICommandHandler<CreateItemCategoryCommand, Result<ItemCategoryResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetItemCategoryById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateItemCategoryRequest request,
        ICommandHandler<UpdateItemCategoryCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateItemCategoryCommand(id, request.Code, request.Name, request.ParentCategoryId);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteItemCategoryCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteItemCategoryCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateItemCategoryRequest(string Code, string Name, Guid? ParentCategoryId);
