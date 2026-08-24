namespace ZARI.Application.Features.Inventory.GoodsIssues.Delete;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record DeleteGoodsIssueCommand(Guid Id) : ICommand<Result>;
