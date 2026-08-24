namespace ZARI.Application.Features.Inventory.GoodsReceipts.RejectCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsReceipts.GetAll;
using ZARI.Domain.Common;

public sealed record RejectGoodsReceiptCancellationCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<GoodsReceiptResponse>>;
