namespace ZARI.Application.Features.Accounting.ExchangeRates.Update;

using FluentValidation;

public sealed class UpdateExchangeRateValidator : AbstractValidator<UpdateExchangeRateCommand>
{
    public UpdateExchangeRateValidator()
    {
        RuleFor(x => x.CurrencyId)
            .NotEmpty()
            .MaximumLength(25);

        RuleFor(x => x.RateToBase)
            .GreaterThan(0);
    }
}
