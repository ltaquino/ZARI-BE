namespace ZARI.Application.Features.Inventory.SerialNumbers.ReverseIssue;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record ReverseIssueSerialCommand(Guid ItemId, string SerialNo) : ICommand<Result>;
