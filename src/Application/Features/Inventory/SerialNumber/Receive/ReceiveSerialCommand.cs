namespace ZARI.Application.Features.Inventory.SerialNumbers.Receive;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.SerialNumbers.GetAll;
using ZARI.Domain.Common;

public sealed record ReceiveSerialCommand(Guid ItemId, string SerialNo, Guid WarehouseId) : ICommand<Result<SerialNumberResponse>>;
