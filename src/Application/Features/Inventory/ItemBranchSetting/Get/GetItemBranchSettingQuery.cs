namespace ZARI.Application.Features.Inventory.ItemBranchSettings.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetItemBranchSettingQuery(Guid Id) : IQuery<Result<ItemBranchSettingResponse>>;

public sealed record ItemBranchSettingResponse(
    Guid Id,
    Guid ItemId,
    string BranchId,
    Guid? DefaultWarehouseId,
    decimal ReorderPoint,
    decimal MinStock,
    decimal MaxStock,
    string Status,
    DateTimeOffset CreatedAt);
