namespace ZARI.Application.Features.Sales.StatutoryDiscountTypes.Update;

using FluentValidation;

public sealed class UpdateStatutoryDiscountTypeValidator : AbstractValidator<UpdateStatutoryDiscountTypeCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public UpdateStatutoryDiscountTypeValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DiscountPct).InclusiveBetween(0, 100);
        RuleFor(x => x.RequiredIdLabel).NotEmpty().MaximumLength(150);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
