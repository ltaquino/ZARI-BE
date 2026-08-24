namespace ZARI.Application.Features.Inventory.GoodsIssues.MarkDelivered;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsIssues.GetAll;
using ZARI.Domain.Common;

public sealed record MarkGoodsIssueDeliveredCommand(Guid Id, string UserId) : ICommand<Result<GoodsIssueResponse>>;
