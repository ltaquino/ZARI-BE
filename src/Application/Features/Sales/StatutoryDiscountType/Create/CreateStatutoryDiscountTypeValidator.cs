namespace ZARI.Application.Features.Sales.StatutoryDiscountTypes.Create;

using FluentValidation;

public sealed class CreateStatutoryDiscountTypeValidator : AbstractValidator<CreateStatutoryDiscountTypeCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public CreateStatutoryDiscountTypeValidator()
    {
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
