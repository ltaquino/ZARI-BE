namespace ZARI.Application.Features.Inventory.StorageLocations.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StorageLocations.Get;
using ZARI.Domain.Common;

public sealed record GetAllStorageLocationsQuery : IQuery<Result<List<StorageLocationResponse>>>;
