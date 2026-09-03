namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.Shared;
using ZARI.Domain.Common;

public sealed class GetAllGoodsReceiptPosQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllGoodsReceiptPosQuery, Result<List<GoodsReceiptPoResponse>>>
{
    public async Task<Result<List<GoodsReceiptPoResponse>>> HandleAsync(GetAllGoodsReceiptPosQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("GOODS_RECEIPT_PO", FormAction.View, cancellationToken))
            return Result.Failure<List<GoodsReceiptPoResponse>>(Error.Forbidden("GoodsReceiptPo.Forbidden", "You do not have permission to view goods receipts (PO)."));

        var receipts = await dbContext.GoodsReceiptPos.AsNoTracking()
            .Include(r => r.Supplier)
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .OrderByDescending(r => r.ReceiptDate)
            .ToListAsync(cancellationToken);

        return Result.Success(receipts.Select(GoodsReceiptPoMapper.ToResponse).ToList());
    }
}
