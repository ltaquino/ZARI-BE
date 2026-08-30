namespace ZARI.Application.Features.Sales.DiscountRules.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateDiscountRuleCommand(
    Guid Id,
    string Code,
    string Name,
    string Scope,
    Guid? ItemId,
    Guid? ItemCategoryId,
    string DiscountType,
    decimal DiscountValue,
    decimal? MinQty,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? BranchId,
    int Priority,
    string Status) : ICommand;
