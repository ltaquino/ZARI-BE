namespace ZARI.Application.Features.RouteSales.Vehicles.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetVehicleQuery(Guid Id) : IQuery<Result<VehicleResponse>>;

/// <summary>A vehicle together with the warehouse and own-series terminal it owns.</summary>
public sealed record VehicleResponse(
    Guid Id,
    string BranchId,
    string Code,
    string PlateNo,
    string? Description,
    string Status,
    Guid WarehouseId,
    string WarehouseCode,
    Guid PosTerminalId,
    string PosTerminalCode,
    string? OrPrefix,
    int OrNextNumber,
    int OrPaddingLength,
    string? MachineIdentificationNumber,
    string? MachineSerialNumber,
    string? PosPermitNumber,
    DateTime? PosPermitDateIssued,
    DateTimeOffset CreatedAt);
