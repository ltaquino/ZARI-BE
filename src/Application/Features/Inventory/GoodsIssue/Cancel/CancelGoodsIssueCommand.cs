namespace ZARI.Application.Features.Inventory.GoodsIssues.Cancel;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsIssues.GetAll;
using ZARI.Domain.Common;

public sealed record CancelGoodsIssueCommand(Guid Id, string CancelledBy, string Reason) : ICommand<Result<GoodsIssueResponse>>;
