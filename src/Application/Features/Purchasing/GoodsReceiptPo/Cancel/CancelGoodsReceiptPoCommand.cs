namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.Cancel;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.GetAll;
using ZARI.Domain.Common;

public sealed record CancelGoodsReceiptPoCommand(Guid Id, string CancelledBy, string Reason) : ICommand<Result<GoodsReceiptPoResponse>>;
