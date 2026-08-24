namespace ZARI.Application.Features.Inventory.ItemCategories.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateItemCategoryCommand(Guid Id, string Code, string Name, Guid? ParentCategoryId) : ICommand;
