namespace ZARI.Application.Features.Inventory.ItemCategories.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteItemCategoryCommand(Guid Id) : ICommand;
