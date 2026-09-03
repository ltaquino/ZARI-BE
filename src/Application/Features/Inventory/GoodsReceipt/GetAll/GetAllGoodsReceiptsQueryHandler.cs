namespace ZARI.Application.Features.Inventory.GoodsReceipts.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsReceipts.Shared;
using ZARI.Domain.Common;

public sealed class GetAllGoodsReceiptsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllGoodsReceiptsQuery, Result<List<GoodsReceiptResponse>>>
{
    public async Task<Result<List<GoodsReceiptResponse>>> HandleAsync(GetAllGoodsReceiptsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("GOODS_RECEIPTS", FormAction.View, cancellationToken))
            return Result.Failure<List<GoodsReceiptResponse>>(Error.Forbidden("GoodsReceipt.Forbidden", "You do not have permission to view goods receipts."));

        var receipts = await dbContext.GoodsReceipts.AsNoTracking()
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .OrderByDescending(r => r.GrDate)
            .ToListAsync(cancellationToken);

        return Result.Success(receipts.Select(GoodsReceiptMapper.ToResponse).ToList());
    }
}
