namespace ZARI.Application.Features.Inventory.ItemCategories.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetItemCategoryQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetItemCategoryQuery, Result<ItemCategoryResponse>>
{
    public async Task<Result<ItemCategoryResponse>> HandleAsync(GetItemCategoryQuery query, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.ItemCategories
            .Where(c => c.Id == query.Id)
            .Select(c => new ItemCategoryResponse(c.Id, c.Code, c.Name, c.ParentCategoryId, c.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (category is null)
            return Result.Failure<ItemCategoryResponse>(Error.NotFound("ItemCategory.NotFound", $"Item category with ID '{query.Id}' was not found."));

        return Result.Success(category);
    }
}
