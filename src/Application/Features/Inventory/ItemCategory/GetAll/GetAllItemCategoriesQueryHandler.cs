namespace ZARI.Application.Features.Inventory.ItemCategories.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.ItemCategories.Get;
using ZARI.Domain.Common;

public sealed class GetAllItemCategoriesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllItemCategoriesQuery, Result<List<ItemCategoryResponse>>>
{
    public async Task<Result<List<ItemCategoryResponse>>> HandleAsync(GetAllItemCategoriesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("ITEM_CATEGORIES", FormAction.View, cancellationToken))
            return Result.Failure<List<ItemCategoryResponse>>(Error.Forbidden("ItemCategory.Forbidden", "You do not have permission to view item categories."));

        var items = await dbContext.ItemCategories.AsNoTracking()
            .OrderBy(c => c.Code)
            .Select(c => new ItemCategoryResponse(c.Id, c.Code, c.Name, c.ParentCategoryId, c.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
