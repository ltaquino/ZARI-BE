namespace ZARI.Application.Features.Purchasing.PurchaseOrders.Delete;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record DeletePurchaseOrderCommand(Guid Id) : ICommand<Result>;
