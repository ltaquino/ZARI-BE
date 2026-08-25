namespace ZARI.Application.Features.Purchasing.PurchaseOrders.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseOrders.GetAll;
using ZARI.Domain.Common;

public sealed record GetPurchaseOrderQuery(Guid Id) : IQuery<Result<PurchaseOrderResponse>>;
