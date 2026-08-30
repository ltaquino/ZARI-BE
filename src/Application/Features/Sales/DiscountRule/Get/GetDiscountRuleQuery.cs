namespace ZARI.Application.Features.Sales.DiscountRules.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetDiscountRuleQuery(Guid Id) : IQuery<Result<DiscountRuleResponse>>;

public sealed record DiscountRuleResponse(
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
    string Status,
    DateTimeOffset CreatedAt);
