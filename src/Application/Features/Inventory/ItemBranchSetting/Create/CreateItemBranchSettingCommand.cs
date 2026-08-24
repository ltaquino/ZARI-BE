namespace ZARI.Application.Features.Inventory.ItemBranchSettings.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.ItemBranchSettings.Get;
using ZARI.Domain.Common;

public sealed record CreateItemBranchSettingCommand(
    Guid ItemId,
    string BranchId,
    Guid? DefaultWarehouseId,
    decimal ReorderPoint,
    decimal MinStock,
    decimal MaxStock,
    string Status) : ICommand<Result<ItemBranchSettingResponse>>;
