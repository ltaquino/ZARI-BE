namespace ZARI.Application.Features.RouteSales.DeliveryRoutes.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteDeliveryRouteCommand(Guid Id) : ICommand;
