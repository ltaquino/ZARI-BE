namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.GetAll;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.Shared;
using ZARI.Domain.Common;

public sealed class GetGoodsReceiptPoQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetGoodsReceiptPoQuery, Result<GoodsReceiptPoResponse>>
{
    public async Task<Result<GoodsReceiptPoResponse>> HandleAsync(GetGoodsReceiptPoQuery query, CancellationToken cancellationToken = default)
    {
        var receipt = await dbContext.GoodsReceiptPos
            .Include(r => r.Supplier)
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == query.Id, cancellationToken);

        if (receipt is null)
            return Result.Failure<GoodsReceiptPoResponse>(Error.NotFound("GoodsReceiptPo.NotFound", $"Goods receipt (PO) with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("GOODS_RECEIPT_PO", FormAction.View, receipt.BranchId, cancellationToken))
            return Result.Failure<GoodsReceiptPoResponse>(Error.Forbidden("GoodsReceiptPo.Forbidden", "You do not have permission to view goods receipts (PO) for this branch."));

        return Result.Success(GoodsReceiptPoMapper.ToResponse(receipt));
    }
}
