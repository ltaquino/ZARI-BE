namespace ZARI.Application.Features.Sales.DeliveryOrders.Delete;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record DeleteDeliveryOrderCommand(Guid Id) : ICommand<Result>;
