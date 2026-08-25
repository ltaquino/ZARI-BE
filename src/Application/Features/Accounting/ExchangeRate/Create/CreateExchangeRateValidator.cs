namespace ZARI.Application.Features.Accounting.ExchangeRates.Create;

using FluentValidation;

public sealed class CreateExchangeRateValidator : AbstractValidator<CreateExchangeRateCommand>
{
    public CreateExchangeRateValidator()
    {
        RuleFor(x => x.CurrencyId)
            .NotEmpty()
            .MaximumLength(25);

        RuleFor(x => x.RateToBase)
            .GreaterThan(0);
    }
}
