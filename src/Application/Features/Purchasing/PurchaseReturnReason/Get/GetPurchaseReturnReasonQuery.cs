namespace ZARI.Application.Features.Purchasing.PurchaseReturnReasons.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetPurchaseReturnReasonQuery(Guid Id) : IQuery<Result<PurchaseReturnReasonResponse>>;

public sealed record PurchaseReturnReasonResponse(Guid Id, string Code, string? Description, string Status, DateTimeOffset CreatedAt);
