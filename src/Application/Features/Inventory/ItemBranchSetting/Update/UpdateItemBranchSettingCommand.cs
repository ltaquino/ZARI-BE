namespace ZARI.Application.Features.Inventory.ItemBranchSettings.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateItemBranchSettingCommand(
    Guid Id,
    Guid ItemId,
    string BranchId,
    Guid? DefaultWarehouseId,
    decimal ReorderPoint,
    decimal MinStock,
    decimal MaxStock,
    string Status) : ICommand;
