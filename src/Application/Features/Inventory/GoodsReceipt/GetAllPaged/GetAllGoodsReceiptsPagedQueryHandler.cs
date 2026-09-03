namespace ZARI.Application.Features.Inventory.GoodsReceipts.GetAllPaged;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsReceipts.GetAll;
using ZARI.Application.Features.Inventory.GoodsReceipts.Shared;
using ZARI.Domain.Common;

public sealed class GetAllGoodsReceiptsPagedQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllGoodsReceiptsPagedQuery, Result<PagedResult<GoodsReceiptResponse>>>
{
    public async Task<Result<PagedResult<GoodsReceiptResponse>>> HandleAsync(GetAllGoodsReceiptsPagedQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("GOODS_RECEIPTS", FormAction.View, cancellationToken))
            return Result.Failure<PagedResult<GoodsReceiptResponse>>(Error.Forbidden("GoodsReceipt.Forbidden", "You do not have permission to view goods receipts."));

        var baseQuery = dbContext.GoodsReceipts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(x => x.GrNo.Contains(query.Search));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var receipts = await baseQuery
            .OrderByDescending(r => r.GrDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<GoodsReceiptResponse>(receipts.Select(GoodsReceiptMapper.ToResponse).ToList(), totalCount, query.Page, query.PageSize));
    }
}
