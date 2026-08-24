namespace ZARI.Application.Features.Inventory.GoodsReceipts.ApproveCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsReceipts.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveGoodsReceiptCancellationCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<GoodsReceiptResponse>>;
