namespace ZARI.Application.Features.RouteSales.DeliveryRoutes.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.DeliveryRoutes.Get;
using ZARI.Domain.Common;

public sealed record GetAllDeliveryRoutesQuery(string? BranchId) : IQuery<Result<List<DeliveryRouteResponse>>>;
