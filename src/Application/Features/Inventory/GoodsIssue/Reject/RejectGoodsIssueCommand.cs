namespace ZARI.Application.Features.Inventory.GoodsIssues.Reject;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsIssues.GetAll;
using ZARI.Domain.Common;

public sealed record RejectGoodsIssueCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<GoodsIssueResponse>>;
