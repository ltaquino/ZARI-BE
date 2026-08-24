namespace ZARI.Application.Features.Inventory.GoodsReceipts.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsReceipts.GetAll;
using ZARI.Application.Features.Inventory.GoodsReceipts.Shared;
using ZARI.Domain.Common;

public sealed class GetGoodsReceiptQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetGoodsReceiptQuery, Result<GoodsReceiptResponse>>
{
    public async Task<Result<GoodsReceiptResponse>> HandleAsync(GetGoodsReceiptQuery query, CancellationToken cancellationToken = default)
    {
        var receipt = await dbContext.GoodsReceipts
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == query.Id, cancellationToken);

        if (receipt is null)
            return Result.Failure<GoodsReceiptResponse>(Error.NotFound("GoodsReceipt.NotFound", $"Goods receipt with ID '{query.Id}' was not found."));

        return Result.Success(GoodsReceiptMapper.ToResponse(receipt));
    }
}
