namespace ZARI.Application.Features.Inventory.GoodsReceipts.Delete;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record DeleteGoodsReceiptCommand(Guid Id) : ICommand<Result>;
