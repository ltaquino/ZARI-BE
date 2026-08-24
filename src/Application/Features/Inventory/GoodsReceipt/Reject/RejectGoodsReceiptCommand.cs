namespace ZARI.Application.Features.Inventory.GoodsReceipts.Reject;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsReceipts.GetAll;
using ZARI.Domain.Common;

public sealed record RejectGoodsReceiptCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<GoodsReceiptResponse>>;
