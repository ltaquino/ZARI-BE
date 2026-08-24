namespace ZARI.Application.Features.Inventory.Items.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteItemCommand(Guid Id) : ICommand;
