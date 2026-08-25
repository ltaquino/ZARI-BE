namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.Delete;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record DeleteGoodsReceiptPoCommand(Guid Id) : ICommand<Result>;
