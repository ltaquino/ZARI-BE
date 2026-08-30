namespace ZARI.Application.Features.Inventory.Items.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateItemCommand(
    Guid Id,
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
    string Status) : ICommand;
