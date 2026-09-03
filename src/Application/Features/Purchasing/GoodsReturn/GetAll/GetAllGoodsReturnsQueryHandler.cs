namespace ZARI.Application.Features.Purchasing.GoodsReturns.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReturns.Shared;
using ZARI.Domain.Common;

public sealed class GetAllGoodsReturnsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllGoodsReturnsQuery, Result<List<GoodsReturnResponse>>>
{
    public async Task<Result<List<GoodsReturnResponse>>> HandleAsync(GetAllGoodsReturnsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("GOODS_RETURNS", FormAction.View, cancellationToken))
            return Result.Failure<List<GoodsReturnResponse>>(Error.Forbidden("GoodsReturn.Forbidden", "You do not have permission to view goods returns."));

        var returns = await dbContext.GoodsReturns.AsNoTracking()
            .Include(r => r.Supplier)
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .OrderByDescending(r => r.ReturnDate)
            .ToListAsync(cancellationToken);

        return Result.Success(returns.Select(GoodsReturnMapper.ToResponse).ToList());
    }
}
