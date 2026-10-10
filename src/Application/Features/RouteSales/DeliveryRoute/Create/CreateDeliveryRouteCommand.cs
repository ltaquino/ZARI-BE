namespace ZARI.Application.Features.RouteSales.DeliveryRoutes.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.DeliveryRoutes.Get;
using ZARI.Application.Features.RouteSales.DeliveryRoutes.Shared;
using ZARI.Domain.Common;

public sealed record CreateDeliveryRouteCommand(
    string BranchId,
    string Code,
    string Name,
    string? Description,
    Guid? DefaultVehicleId,
    string Status,
    List<DeliveryRouteStopInput> Stops) : ICommand<Result<DeliveryRouteResponse>>;
