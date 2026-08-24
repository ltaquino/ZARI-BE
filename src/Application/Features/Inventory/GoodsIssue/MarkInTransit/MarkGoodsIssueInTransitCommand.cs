namespace ZARI.Application.Features.Inventory.GoodsIssues.MarkInTransit;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsIssues.GetAll;
using ZARI.Domain.Common;

public sealed record MarkGoodsIssueInTransitCommand(Guid Id, string UserId) : ICommand<Result<GoodsIssueResponse>>;
