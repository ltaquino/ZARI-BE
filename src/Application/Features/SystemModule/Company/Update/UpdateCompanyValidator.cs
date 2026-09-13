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

        // CIC's own format for a submitting entity's provider code (RA 9510 CSDF spec) — 8
        // alphanumeric characters. Optional: null until the cooperative's real code is known.
        RuleFor(x => x.CicProviderCode)
            .Matches("^[A-Za-z0-9]{8}$")
            .When(x => !string.IsNullOrWhiteSpace(x.CicProviderCode))
            .WithMessage("CIC provider code must be exactly 8 alphanumeric characters.");
    }
}
