namespace ZARI.Application.Features.Sales.DiscountRules.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteDiscountRuleCommand(Guid Id) : ICommand;
