namespace ZARI.Application.Features.Inventory.GoodsIssues.RequestCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsIssues.GetAll;
using ZARI.Domain.Common;

public sealed record RequestGoodsIssueCancellationCommand(Guid Id, string RequestedBy, string Reason) : ICommand<Result<GoodsIssueResponse>>;
