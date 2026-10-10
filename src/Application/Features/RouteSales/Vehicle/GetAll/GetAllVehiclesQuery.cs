namespace ZARI.Application.Features.RouteSales.Vehicles.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.Vehicles.Get;
using ZARI.Domain.Common;

/// <summary>Optional BranchId narrows to one branch; View permission is still checked per row.</summary>
public sealed record GetAllVehiclesQuery(string? BranchId) : IQuery<Result<List<VehicleResponse>>>;
