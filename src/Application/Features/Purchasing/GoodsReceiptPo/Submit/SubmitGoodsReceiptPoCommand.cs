namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.Submit;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.GetAll;
using ZARI.Domain.Common;

public sealed record SubmitGoodsReceiptPoCommand(Guid Id, string RequestedBy) : ICommand<Result<GoodsReceiptPoResponse>>;
