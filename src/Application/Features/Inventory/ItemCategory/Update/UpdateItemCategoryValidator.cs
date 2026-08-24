namespace ZARI.Application.Features.Inventory.ItemCategories.Update;

using FluentValidation;

public sealed class UpdateItemCategoryValidator : AbstractValidator<UpdateItemCategoryCommand>
{
    public UpdateItemCategoryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(25);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);
    }
}
