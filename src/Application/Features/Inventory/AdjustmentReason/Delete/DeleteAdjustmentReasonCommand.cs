namespace ZARI.Application.Features.Inventory.AdjustmentReasons.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteAdjustmentReasonCommand(Guid Id) : ICommand;
