using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.ItemBarcodes;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class ItemBarcodeEndpoints
{
    public static void MapItemBarcodeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/item-barcodes")
            .WithTags("ItemBarcodes")
            .WithGroupName("Inventory")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetItemBarcodes")
            .WithSummary("Item barcodes — one item's (itemId), or all; updatedSince for delta sync");

        group.MapGet("/lookup", Lookup)
            .WithName("LookupItemBarcode")
            .WithSummary("Resolve a scanned barcode to its item and pack quantity");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateItemBarcodeCommand>>()
            .WithName("CreateItemBarcode")
            .WithSummary("Add a barcode to an item");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteItemBarcode")
            .WithSummary("Remove an item barcode");
    }

    private static async Task<IResult> GetAll(
        Guid? itemId,
        DateTimeOffset? updatedSince,
        IQueryHandler<GetItemBarcodesQuery, Result<List<ItemBarcodeResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetItemBarcodesQuery(itemId, updatedSince), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Lookup(
        string code,
        IQueryHandler<LookupItemBarcodeQuery, Result<ItemBarcodeLookupResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new LookupItemBarcodeQuery(code), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateItemBarcodeCommand command,
        ICommandHandler<CreateItemBarcodeCommand, Result<ItemBarcodeResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteItemBarcodeCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteItemBarcodeCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
