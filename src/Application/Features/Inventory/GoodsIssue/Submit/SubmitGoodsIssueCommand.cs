namespace ZARI.Application.Features.Inventory.GoodsIssues.Submit;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsIssues.GetAll;
using ZARI.Domain.Common;

public sealed record SubmitGoodsIssueCommand(Guid Id, string RequestedBy) : ICommand<Result<GoodsIssueResponse>>;
