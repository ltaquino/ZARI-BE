namespace ZARI.Application.Features.Inventory.GoodsIssues.Approve;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsIssues.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveGoodsIssueCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<GoodsIssueResponse>>;
