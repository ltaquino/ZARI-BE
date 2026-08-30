namespace ZARI.Application.Features.Sales.DiscountRules.Create;

using FluentValidation;

public sealed class CreateDiscountRuleValidator : AbstractValidator<CreateDiscountRuleCommand>
{
    private static readonly string[] ValidScopes = ["ITEM", "CATEGORY", "ALL"];
    private static readonly string[] ValidDiscountTypes = ["PERCENT", "FIXED_AMOUNT"];
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public CreateDiscountRuleValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DiscountValue).GreaterThan(0);
        RuleFor(x => x.MinQty).GreaterThan(0).When(x => x.MinQty.HasValue);
        RuleFor(x => x.BranchId).MaximumLength(25);
        RuleFor(x => x.Priority).GreaterThanOrEqualTo(0);

        RuleFor(x => x.Scope)
            .NotEmpty()
            .Must(s => ValidScopes.Contains(s))
            .WithMessage($"Scope must be one of: {string.Join(", ", ValidScopes)}.");

        RuleFor(x => x.ItemId).NotEmpty().When(x => x.Scope == "ITEM")
            .WithMessage("ItemId is required when Scope is 'ITEM'.");
        RuleFor(x => x.ItemCategoryId).NotEmpty().When(x => x.Scope == "CATEGORY")
            .WithMessage("ItemCategoryId is required when Scope is 'CATEGORY'.");

        RuleFor(x => x.DiscountType)
            .NotEmpty()
            .Must(t => ValidDiscountTypes.Contains(t))
            .WithMessage($"Discount type must be one of: {string.Join(", ", ValidDiscountTypes)}.");

        RuleFor(x => x.DiscountValue)
            .LessThanOrEqualTo(100)
            .When(x => x.DiscountType == "PERCENT")
            .WithMessage("A PERCENT discount value cannot exceed 100.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("EndDate must be on or after StartDate.");

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
