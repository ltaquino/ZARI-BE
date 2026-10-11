namespace ZARI.Application.Features.Inventory.ItemBarcodes;

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Items.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed record ItemBarcodeResponse(Guid Id, Guid ItemId, string ItemCode, string Barcode, decimal PackQty, string? Description, DateTimeOffset CreatedAt);

/// <summary>What a scan resolves to: the item, and how many of its base unit the scanned pack holds.</summary>
public sealed record ItemBarcodeLookupResponse(ItemResponse Item, string Barcode, decimal PackQty, string? Description);

internal static class ItemBarcodeShared
{
    public static IQueryable<ItemBarcodeResponse> Project(IQueryable<ItemBarcode> barcodes) => barcodes
        .OrderBy(b => b.Item.Code).ThenBy(b => b.PackQty).ThenBy(b => b.Barcode)
        .Select(b => new ItemBarcodeResponse(b.Id, b.ItemId, b.Item.Code, b.Barcode, b.PackQty, b.Description, b.CreatedAt));

    public static Error Forbidden(string verb) => Error.Forbidden("ItemBarcode.Forbidden", $"You do not have permission to {verb} item barcodes.");
}

// ---------------------------------------------------------------- List (one item's, or all — ZARI Mobile downloads them)

/// <param name="ItemId">Only this item's barcodes.</param>
/// <param name="UpdatedSince">Delta sync: only barcodes added or changed at/after this instant.</param>
public sealed record GetItemBarcodesQuery(Guid? ItemId = null, DateTimeOffset? UpdatedSince = null) : IQuery<Result<List<ItemBarcodeResponse>>>;

public sealed class GetItemBarcodesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetItemBarcodesQuery, Result<List<ItemBarcodeResponse>>>
{
    public async Task<Result<List<ItemBarcodeResponse>>> HandleAsync(GetItemBarcodesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("ITEMS", FormAction.View, cancellationToken))
            return Result.Failure<List<ItemBarcodeResponse>>(ItemBarcodeShared.Forbidden("view"));

        var barcodes = dbContext.ItemBarcodes.AsNoTracking();
        if (query.ItemId is { } itemId)
            barcodes = barcodes.Where(b => b.ItemId == itemId);
        if (query.UpdatedSince is { } since)
            barcodes = barcodes.Where(b => b.CreatedAt >= since || b.LastModifiedAt >= since);

        return Result.Success(await ItemBarcodeShared.Project(barcodes).ToListAsync(cancellationToken));
    }
}

// ---------------------------------------------------------------- Lookup: what a scanner typed

public sealed record LookupItemBarcodeQuery(string Code) : IQuery<Result<ItemBarcodeLookupResponse>>;

public sealed class LookupItemBarcodeQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<LookupItemBarcodeQuery, Result<ItemBarcodeLookupResponse>>
{
    public async Task<Result<ItemBarcodeLookupResponse>> HandleAsync(LookupItemBarcodeQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("ITEMS", FormAction.View, cancellationToken))
            return Result.Failure<ItemBarcodeLookupResponse>(ItemBarcodeShared.Forbidden("view"));

        var code = query.Code?.Trim() ?? "";
        var match = code.Length == 0 ? null : await dbContext.ItemBarcodes.AsNoTracking()
            .Where(b => b.Barcode == code && b.Item.Status == "active")
            .Select(b => new ItemBarcodeLookupResponse(
                new ItemResponse(
                    b.Item.Id, b.Item.Code, b.Item.Name, b.Item.Description, b.Item.CategoryId, b.Item.BaseUomId, b.Item.ItemType, b.Item.CostingMethod,
                    b.Item.IsSerialized, b.Item.IsBatchTracked, b.Item.IsSold, b.Item.IsPurchased, b.Item.IsStocked, b.Item.IsTileDisplay, b.Item.AllowNegativeStock,
                    b.Item.SalesAccountId, b.Item.PurchaseAccountId, b.Item.InventoryAccountId, b.Item.CogsAccountId,
                    b.Item.VatType, b.Item.Status, b.Item.CreatedAt),
                b.Barcode, b.PackQty, b.Description))
            .FirstOrDefaultAsync(cancellationToken);

        return match is null
            ? Result.Failure<ItemBarcodeLookupResponse>(Error.NotFound("ItemBarcode.NotFound", $"No active item has the barcode '{code}'."))
            : Result.Success(match);
    }
}

// ---------------------------------------------------------------- Add

public sealed record CreateItemBarcodeCommand(Guid ItemId, string Barcode, decimal PackQty = 1, string? Description = null)
    : ICommand<Result<ItemBarcodeResponse>>;

public sealed class CreateItemBarcodeValidator : AbstractValidator<CreateItemBarcodeCommand>
{
    public CreateItemBarcodeValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        // What a scanner can type: letters, digits and the few symbols Code 128/Code 39 carry — no spaces.
        RuleFor(x => x.Barcode).NotEmpty().MaximumLength(50).Matches(@"^[A-Za-z0-9\-\.\/\+]+$")
            .WithMessage("A barcode can only contain letters, digits and - . / + (no spaces).");
        RuleFor(x => x.PackQty).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(100);
    }
}

public sealed class CreateItemBarcodeCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<CreateItemBarcodeCommand, Result<ItemBarcodeResponse>>
{
    public async Task<Result<ItemBarcodeResponse>> HandleAsync(CreateItemBarcodeCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("ITEMS", FormAction.Edit, cancellationToken))
            return Result.Failure<ItemBarcodeResponse>(ItemBarcodeShared.Forbidden("add"));

        var item = await dbContext.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == command.ItemId, cancellationToken);
        if (item is null)
            return Result.Failure<ItemBarcodeResponse>(Error.NotFound("Item.NotFound", $"Item with ID '{command.ItemId}' was not found."));

        var barcode = command.Barcode.Trim();
        var taken = await dbContext.ItemBarcodes.AsNoTracking().Where(b => b.Barcode == barcode).Select(b => b.Item.Code).FirstOrDefaultAsync(cancellationToken);
        if (taken is not null)
            return Result.Failure<ItemBarcodeResponse>(Error.Conflict("ItemBarcode.Duplicate", $"Barcode {barcode} already belongs to item {taken}."));

        var row = new ItemBarcode
        {
            ItemId = item.Id,
            Barcode = barcode,
            PackQty = command.PackQty,
            Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim(),
        };
        dbContext.ItemBarcodes.Add(row);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new ItemBarcodeResponse(row.Id, row.ItemId, item.Code, row.Barcode, row.PackQty, row.Description, row.CreatedAt));
    }
}

// ---------------------------------------------------------------- Remove

public sealed record DeleteItemBarcodeCommand(Guid Id) : ICommand;

public sealed class DeleteItemBarcodeCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteItemBarcodeCommand>
{
    public async Task<Result> HandleAsync(DeleteItemBarcodeCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("ITEMS", FormAction.Edit, cancellationToken))
            return Result.Failure(ItemBarcodeShared.Forbidden("remove"));

        var row = await dbContext.ItemBarcodes.FindAsync([command.Id], cancellationToken);
        if (row is null)
            return Result.Failure(Error.NotFound("ItemBarcode.NotFound", $"Barcode with ID '{command.Id}' was not found."));

        dbContext.ItemBarcodes.Remove(row);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
