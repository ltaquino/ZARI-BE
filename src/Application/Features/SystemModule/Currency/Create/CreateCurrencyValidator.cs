namespace ZARI.Application.Features.SystemModule.Currencies.Create;

using FluentValidation;

public sealed class CreateCurrencyValidator : AbstractValidator<CreateCurrencyCommand>
{
    public CreateCurrencyValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(25);

        RuleFor(x => x.Name)
            .MaximumLength(150);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => s is "active" or "inactive")
            .WithMessage("Status must be 'active' or 'inactive'.");
    }
}
