namespace ZARI.Application.Features.Inventory.ItemCategories.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.ItemCategories.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateItemCategoryCommandHandler(IAppDbContext dbContext) : ICommandHandler<CreateItemCategoryCommand, Result<ItemCategoryResponse>>
{
    public async Task<Result<ItemCategoryResponse>> HandleAsync(CreateItemCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var codeExists = await dbContext.ItemCategories
            .AnyAsync(c => c.Code == command.Code, cancellationToken);

        if (codeExists)
            return Result.Failure<ItemCategoryResponse>(Error.Conflict("ItemCategory.DuplicateCode", $"An item category with code '{command.Code}' already exists."));

        if (command.ParentCategoryId is not null)
        {
            var parentExists = await dbContext.ItemCategories
                .AnyAsync(c => c.Id == command.ParentCategoryId, cancellationToken);

            if (!parentExists)
                return Result.Failure<ItemCategoryResponse>(Error.NotFound("ItemCategory.ParentNotFound", $"Parent category with ID '{command.ParentCategoryId}' was not found."));
        }

        var category = new ItemCategory
        {
            Code = command.Code,
            Name = command.Name,
            ParentCategoryId = command.ParentCategoryId
        };

        dbContext.ItemCategories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new ItemCategoryResponse(category.Id, category.Code, category.Name, category.ParentCategoryId, category.CreatedAt);
        return Result.Success(response);
    }
}
