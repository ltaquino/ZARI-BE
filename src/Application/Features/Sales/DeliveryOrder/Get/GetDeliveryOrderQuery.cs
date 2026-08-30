namespace ZARI.Application.Features.Sales.DeliveryOrders.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.DeliveryOrders.GetAll;
using ZARI.Domain.Common;

public sealed record GetDeliveryOrderQuery(Guid Id) : IQuery<Result<DeliveryOrderResponse>>;
