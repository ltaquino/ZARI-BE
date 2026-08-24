namespace ZARI.Application.Features.Inventory.SerialNumbers.ReverseReceive;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record ReverseReceiveSerialCommand(Guid ItemId, string SerialNo, string RevertTo) : ICommand<Result>;
