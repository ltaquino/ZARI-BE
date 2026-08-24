namespace ZARI.Application.Features.Inventory.GoodsReceipts.Approve;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsReceipts.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveGoodsReceiptCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<GoodsReceiptResponse>>;
