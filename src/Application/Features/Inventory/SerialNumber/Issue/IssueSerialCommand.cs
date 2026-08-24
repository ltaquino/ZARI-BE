namespace ZARI.Application.Features.Inventory.SerialNumbers.Issue;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record IssueSerialCommand(Guid ItemId, string SerialNo, string Disposition) : ICommand<Result>;
