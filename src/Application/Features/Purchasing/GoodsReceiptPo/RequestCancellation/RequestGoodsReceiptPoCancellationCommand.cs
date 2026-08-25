namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.RequestCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.GetAll;
using ZARI.Domain.Common;

public sealed record RequestGoodsReceiptPoCancellationCommand(Guid Id, string RequestedBy, string Reason) : ICommand<Result<GoodsReceiptPoResponse>>;
