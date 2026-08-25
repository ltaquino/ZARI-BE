namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.RejectCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.GetAll;
using ZARI.Domain.Common;

public sealed record RejectGoodsReceiptPoCancellationCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<GoodsReceiptPoResponse>>;
