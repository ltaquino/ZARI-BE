namespace ZARI.Application.Features.Inventory.ItemBranchSettings.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteItemBranchSettingCommand(Guid Id) : ICommand;
