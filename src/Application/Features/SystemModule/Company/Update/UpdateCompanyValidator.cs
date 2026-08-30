namespace ZARI.Application.Features.SystemModule.Companies.Update;

using FluentValidation;

public sealed class UpdateCompanyValidator : AbstractValidator<UpdateCompanyCommand>
{
    private static readonly string[] ValidVatRegistrationTypes = ["VAT", "NON_VAT"];

    public UpdateCompanyValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.TaxId).MaximumLength(25);
        RuleFor(x => x.BaseCurrencyId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.RegisteredAddress).MaximumLength(300);
        RuleFor(x => x.TradeName).MaximumLength(150);
        RuleFor(x => x.VatRegistrationType)
            .Must(v => v is null || ValidVatRegistrationTypes.Contains(v))
            .WithMessage($"VAT registration type must be one of: {string.Join(", ", ValidVatRegistrationTypes)}.");
        RuleFor(x => x.MaxUnapprovedDiscountPct).InclusiveBetween(0, 100).When(x => x.MaxUnapprovedDiscountPct.HasValue);
    }
}
