namespace ZARI.Application.Features.Purchasing.PurchaseReturnReasons.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdatePurchaseReturnReasonCommand(Guid Id, string Code, string? Description, string Status) : ICommand;
