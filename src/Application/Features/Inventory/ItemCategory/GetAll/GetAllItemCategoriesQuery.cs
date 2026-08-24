namespace ZARI.Application.Features.Inventory.ItemCategories.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.ItemCategories.Get;
using ZARI.Domain.Common;

public sealed record GetAllItemCategoriesQuery : IQuery<Result<List<ItemCategoryResponse>>>;
