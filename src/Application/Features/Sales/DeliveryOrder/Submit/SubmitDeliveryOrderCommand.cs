namespace ZARI.Application.Features.Sales.DeliveryOrders.Submit;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.DeliveryOrders.GetAll;
using ZARI.Domain.Common;

public sealed record SubmitDeliveryOrderCommand(Guid Id, string RequestedBy) : ICommand<Result<DeliveryOrderResponse>>;
