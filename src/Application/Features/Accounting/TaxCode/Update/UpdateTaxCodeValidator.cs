namespace ZARI.Application.Features.Accounting.TaxCodes.Update;

using FluentValidation;

public sealed class UpdateTaxCodeValidator : AbstractValidator<UpdateTaxCodeCommand>
{
    public UpdateTaxCodeValidator()
    {
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
