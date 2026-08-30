namespace ZARI.Application.Features.Sales.DeliveryOrders.RequestCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.DeliveryOrders.GetAll;
using ZARI.Domain.Common;

public sealed record RequestDeliveryOrderCancellationCommand(Guid Id, string RequestedBy, string Reason) : ICommand<Result<DeliveryOrderResponse>>;
