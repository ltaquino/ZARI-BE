namespace ZARI.Application.Features.Sales.DiscountRules.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.DiscountRules.Get;
using ZARI.Domain.Common;

public sealed record GetAllDiscountRulesQuery : IQuery<Result<List<DiscountRuleResponse>>>;
