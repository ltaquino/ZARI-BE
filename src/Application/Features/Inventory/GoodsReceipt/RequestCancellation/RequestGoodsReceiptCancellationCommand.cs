namespace ZARI.Application.Features.Inventory.GoodsReceipts.RequestCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsReceipts.GetAll;
using ZARI.Domain.Common;

public sealed record RequestGoodsReceiptCancellationCommand(Guid Id, string RequestedBy, string Reason) : ICommand<Result<GoodsReceiptResponse>>;
