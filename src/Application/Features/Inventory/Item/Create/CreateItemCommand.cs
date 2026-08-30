namespace ZARI.Application.Features.Inventory.Items.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Items.Get;
using ZARI.Domain.Common;

public sealed record CreateItemCommand(
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
    string VatType,
    string Status) : ICommand<Result<ItemResponse>>;
