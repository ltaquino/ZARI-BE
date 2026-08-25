namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.Approve;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveGoodsReceiptPoCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<GoodsReceiptPoResponse>>;
