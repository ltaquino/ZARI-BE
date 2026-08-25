namespace ZARI.Application.Features.Purchasing.PurchaseOrders.Cancel;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseOrders.GetAll;
using ZARI.Domain.Common;

public sealed record CancelPurchaseOrderCommand(Guid Id, string CancelledBy, string Reason) : ICommand<Result<PurchaseOrderResponse>>;
