namespace ZARI.Application.Features.Inventory.GoodsReceipts.Submit;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsReceipts.GetAll;
using ZARI.Domain.Common;

public sealed record SubmitGoodsReceiptCommand(Guid Id, string RequestedBy) : ICommand<Result<GoodsReceiptResponse>>;
