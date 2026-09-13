namespace ZARI.Application.Features.Hr.WithholdingTaxBrackets.Create;

using FluentValidation;

public sealed class CreateWithholdingTaxBracketValidator : AbstractValidator<CreateWithholdingTaxBracketCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public CreateWithholdingTaxBracketValidator()
    {
        RuleFor(x => x.SalaryFrom).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SalaryTo).GreaterThan(x => x.SalaryFrom);
        RuleFor(x => x.BaseTax).GreaterThanOrEqualTo(0);
        RuleFor(x => x.RateOverExcess).GreaterThanOrEqualTo(0);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
