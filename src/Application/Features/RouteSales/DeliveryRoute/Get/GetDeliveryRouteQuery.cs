namespace ZARI.Application.Features.RouteSales.DeliveryRoutes.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetDeliveryRouteQuery(Guid Id) : IQuery<Result<DeliveryRouteResponse>>;

public sealed record DeliveryRouteStopResponse(Guid Id, Guid CustomerId, string CustomerName, int Sequence, string? Notes);

public sealed record DeliveryRouteResponse(
    Guid Id,
    string BranchId,
    string Code,
    string Name,
    string? Description,
    Guid? DefaultVehicleId,
    string? DefaultVehicleCode,
    string Status,
    List<DeliveryRouteStopResponse> Stops,
    DateTimeOffset CreatedAt);
