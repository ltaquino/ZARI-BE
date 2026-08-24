namespace ZARI.Application.Features.Inventory.ItemCategories.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.ItemCategories.Get;
using ZARI.Domain.Common;

public sealed record CreateItemCategoryCommand(string Code, string Name, Guid? ParentCategoryId) : ICommand<Result<ItemCategoryResponse>>;
