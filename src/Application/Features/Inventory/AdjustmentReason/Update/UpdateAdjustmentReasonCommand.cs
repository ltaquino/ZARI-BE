namespace ZARI.Application.Features.Inventory.AdjustmentReasons.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateAdjustmentReasonCommand(Guid Id, string Code, string? Description, string? GlAccountId, string Status) : ICommand;
