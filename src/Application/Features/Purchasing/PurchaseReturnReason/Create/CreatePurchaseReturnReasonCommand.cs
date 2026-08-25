namespace ZARI.Application.Features.Purchasing.PurchaseReturnReasons.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseReturnReasons.Get;
using ZARI.Domain.Common;

public sealed record CreatePurchaseReturnReasonCommand(string Code, string? Description, string Status) : ICommand<Result<PurchaseReturnReasonResponse>>;
