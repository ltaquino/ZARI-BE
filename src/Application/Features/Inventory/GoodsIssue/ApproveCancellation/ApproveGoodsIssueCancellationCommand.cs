namespace ZARI.Application.Features.Inventory.GoodsIssues.ApproveCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsIssues.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveGoodsIssueCancellationCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<GoodsIssueResponse>>;
