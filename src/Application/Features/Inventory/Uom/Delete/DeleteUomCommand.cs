namespace ZARI.Application.Features.Inventory.Uoms.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteUomCommand(Guid Id) : ICommand;
