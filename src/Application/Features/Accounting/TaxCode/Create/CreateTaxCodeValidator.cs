namespace ZARI.Application.Features.Accounting.TaxCodes.Create;

using FluentValidation;

public sealed class CreateTaxCodeValidator : AbstractValidator<CreateTaxCodeCommand>
{
    public CreateTaxCodeValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(25);

        RuleFor(x => x.Name)
            .MaximumLength(150);

        RuleFor(x => x.Rate)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.TaxType)
            .NotEmpty()
            .Must(t => t is "Vat" or "Withholding")
            .WithMessage("TaxType must be 'Vat' or 'Withholding'.");
    }
}
