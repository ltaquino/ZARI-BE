namespace ZARI.Application.Features.SystemModule.Currencies.Update;

using FluentValidation;

public sealed class UpdateCurrencyValidator : AbstractValidator<UpdateCurrencyCommand>
{
    public UpdateCurrencyValidator()
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
