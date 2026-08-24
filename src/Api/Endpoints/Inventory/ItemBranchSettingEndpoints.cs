using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.ItemBranchSettings.Create;
using ZARI.Application.Features.Inventory.ItemBranchSettings.Delete;
using ZARI.Application.Features.Inventory.ItemBranchSettings.Get;
using ZARI.Application.Features.Inventory.ItemBranchSettings.GetAll;
using ZARI.Application.Features.Inventory.ItemBranchSettings.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class ItemBranchSettingEndpoints
{
    public static void MapItemBranchSettingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/item-branch-settings")
            .WithTags("ItemBranchSettings")
            .WithGroupName("Inventory")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllItemBranchSettings")
            .WithSummary("Get all reorder settings");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetItemBranchSettingById")
            .WithSummary("Get a reorder setting by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateItemBranchSettingCommand>>()
            .WithName("CreateItemBranchSetting")
            .WithSummary("Create a new reorder setting");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateItemBranchSetting")
            .WithSummary("Update an existing reorder setting");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteItemBranchSetting")
            .WithSummary("Delete a reorder setting");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllItemBranchSettingsQuery, Result<List<ItemBranchSettingResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllItemBranchSettingsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetItemBranchSettingQuery, Result<ItemBranchSettingResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetItemBranchSettingQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateItemBranchSettingCommand command,
        ICommandHandler<CreateItemBranchSettingCommand, Result<ItemBranchSettingResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetItemBranchSettingById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateItemBranchSettingRequest request,
        IValidator<UpdateItemBranchSettingCommand> validator,
        ICommandHandler<UpdateItemBranchSettingCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateItemBranchSettingCommand(
            id, request.ItemId, request.BranchId, request.DefaultWarehouseId, request.ReorderPoint, request.MinStock, request.MaxStock, request.Status);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteItemBranchSettingCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteItemBranchSettingCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateItemBranchSettingRequest(
    Guid ItemId, string BranchId, Guid? DefaultWarehouseId, decimal ReorderPoint, decimal MinStock, decimal MaxStock, string Status);
