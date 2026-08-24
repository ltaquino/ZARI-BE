namespace ZARI.Application.Features.Inventory.GoodsReceipts.Cancel;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsReceipts.GetAll;
using ZARI.Domain.Common;

public sealed record CancelGoodsReceiptCommand(Guid Id, string CancelledBy, string Reason) : ICommand<Result<GoodsReceiptResponse>>;
