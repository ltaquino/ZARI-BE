namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.ApproveCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveGoodsReceiptPoCancellationCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<GoodsReceiptPoResponse>>;
