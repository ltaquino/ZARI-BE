namespace ZARI.Application.Features.SystemModule.Companies.Update;

using FluentValidation;

public sealed class UpdateCompanyValidator : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.TaxId).MaximumLength(25);
        RuleFor(x => x.BaseCurrencyId).NotEmpty().MaximumLength(25);
    }
}
