namespace ZARI.Application.Features.Inventory.ItemBranchSettings.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.ItemBranchSettings.Get;
using ZARI.Domain.Common;

public sealed record GetAllItemBranchSettingsQuery : IQuery<Result<List<ItemBranchSettingResponse>>>;
