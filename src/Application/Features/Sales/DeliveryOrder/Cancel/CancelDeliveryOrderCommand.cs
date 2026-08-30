namespace ZARI.Application.Features.Sales.DeliveryOrders.Cancel;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.DeliveryOrders.GetAll;
using ZARI.Domain.Common;

public sealed record CancelDeliveryOrderCommand(Guid Id, string CancelledBy, string Reason) : ICommand<Result<DeliveryOrderResponse>>;
