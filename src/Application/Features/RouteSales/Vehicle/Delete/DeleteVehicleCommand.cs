namespace ZARI.Application.Features.RouteSales.Vehicles.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteVehicleCommand(Guid Id) : ICommand;
