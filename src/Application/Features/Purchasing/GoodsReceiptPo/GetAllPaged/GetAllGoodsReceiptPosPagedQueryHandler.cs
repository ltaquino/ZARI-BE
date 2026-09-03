namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.GetAllPaged;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.GetAll;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.Shared;
using ZARI.Domain.Common;

public sealed class GetAllGoodsReceiptPosPagedQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllGoodsReceiptPosPagedQuery, Result<PagedResult<GoodsReceiptPoResponse>>>
{
    public async Task<Result<PagedResult<GoodsReceiptPoResponse>>> HandleAsync(GetAllGoodsReceiptPosPagedQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("GOODS_RECEIPT_PO", FormAction.View, cancellationToken))
            return Result.Failure<PagedResult<GoodsReceiptPoResponse>>(Error.Forbidden("GoodsReceiptPo.Forbidden", "You do not have permission to view goods receipt pos."));

        var baseQuery = dbContext.GoodsReceiptPos.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(x => x.GrpoNo.Contains(query.Search));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var receipts = await baseQuery
            .OrderByDescending(r => r.ReceiptDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(r => r.Supplier)
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<GoodsReceiptPoResponse>(receipts.Select(GoodsReceiptPoMapper.ToResponse).ToList(), totalCount, query.Page, query.PageSize));
    }
}
