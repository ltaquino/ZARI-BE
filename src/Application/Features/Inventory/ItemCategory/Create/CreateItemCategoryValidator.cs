namespace ZARI.Application.Features.Inventory.ItemCategories.Create;

using FluentValidation;

public sealed class CreateItemCategoryValidator : AbstractValidator<CreateItemCategoryCommand>
{
    public CreateItemCategoryValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(25);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);
    }
}
