namespace ZARI.Application.Features.Sales.DiscountRules.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.DiscountRules.Get;
using ZARI.Domain.Common;

public sealed record CreateDiscountRuleCommand(
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
    string Status) : ICommand<Result<DiscountRuleResponse>>;
