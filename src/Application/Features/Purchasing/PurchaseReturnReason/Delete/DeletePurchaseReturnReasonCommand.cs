namespace ZARI.Application.Features.Purchasing.PurchaseReturnReasons.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeletePurchaseReturnReasonCommand(Guid Id) : ICommand;
