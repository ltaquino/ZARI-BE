namespace ZARI.Application.Features.Inventory.ItemBranchSettings.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetItemBranchSettingQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetItemBranchSettingQuery, Result<ItemBranchSettingResponse>>
{
    public async Task<Result<ItemBranchSettingResponse>> HandleAsync(GetItemBranchSettingQuery query, CancellationToken cancellationToken = default)
    {
        var setting = await dbContext.ItemBranchSettings
            .Where(s => s.Id == query.Id)
            .Select(s => new ItemBranchSettingResponse(s.Id, s.ItemId, s.BranchId, s.DefaultWarehouseId, s.ReorderPoint, s.MinStock, s.MaxStock, s.Status, s.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (setting is null)
            return Result.Failure<ItemBranchSettingResponse>(Error.NotFound("ItemBranchSetting.NotFound", $"Reorder setting with ID '{query.Id}' was not found."));

        return Result.Success(setting);
    }
}
