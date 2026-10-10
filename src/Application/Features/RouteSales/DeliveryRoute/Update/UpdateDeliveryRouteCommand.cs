namespace ZARI.Application.Features.RouteSales.DeliveryRoutes.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.DeliveryRoutes.Shared;

/// <summary>Stops are replaced wholesale with the given list (its order = visiting order).</summary>
public sealed record UpdateDeliveryRouteCommand(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    Guid? DefaultVehicleId,
    string Status,
    List<DeliveryRouteStopInput> Stops) : ICommand;
