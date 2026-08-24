namespace ZARI.Application.Features.Inventory.ItemCategories.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetItemCategoryQuery(Guid Id) : IQuery<Result<ItemCategoryResponse>>;

public sealed record ItemCategoryResponse(Guid Id, string Code, string Name, Guid? ParentCategoryId, DateTimeOffset CreatedAt);
