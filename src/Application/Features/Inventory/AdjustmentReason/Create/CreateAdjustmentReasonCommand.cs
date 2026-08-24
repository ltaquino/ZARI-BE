namespace ZARI.Application.Features.Inventory.AdjustmentReasons.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.AdjustmentReasons.Get;
using ZARI.Domain.Common;

public sealed record CreateAdjustmentReasonCommand(string Code, string? Description, string? GlAccountId, string Status) : ICommand<Result<AdjustmentReasonResponse>>;
