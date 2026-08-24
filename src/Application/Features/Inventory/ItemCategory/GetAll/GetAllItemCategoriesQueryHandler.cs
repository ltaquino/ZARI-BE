namespace ZARI.Application.Features.Inventory.ItemCategories.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.ItemCategories.Get;
using ZARI.Domain.Common;

public sealed class GetAllItemCategoriesQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetAllItemCategoriesQuery, Result<List<ItemCategoryResponse>>>
{
    public async Task<Result<List<ItemCategoryResponse>>> HandleAsync(GetAllItemCategoriesQuery query, CancellationToken cancellationToken = default)
    {
        var items = await dbContext.ItemCategories
            .OrderBy(c => c.Code)
            .Select(c => new ItemCategoryResponse(c.Id, c.Code, c.Name, c.ParentCategoryId, c.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
