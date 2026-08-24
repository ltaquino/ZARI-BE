namespace ZARI.Application.Features.Inventory.SerialNumbers.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllSerialNumbersQuery : IQuery<Result<List<SerialNumberResponse>>>;

public sealed record SerialNumberResponse(
    Guid Id,
    Guid ItemId,
    string SerialNo,
    Guid? WarehouseId,
    string Status,
    DateTimeOffset CreatedAt);
