namespace ZARI.Application.Features.Inventory.AdjustmentReasons.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAdjustmentReasonQuery(Guid Id) : IQuery<Result<AdjustmentReasonResponse>>;

public sealed record AdjustmentReasonResponse(Guid Id, string Code, string? Description, string? GlAccountId, string Status, DateTimeOffset CreatedAt);
