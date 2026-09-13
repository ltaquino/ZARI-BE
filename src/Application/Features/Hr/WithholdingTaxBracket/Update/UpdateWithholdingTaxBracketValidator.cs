namespace ZARI.Application.Features.Hr.WithholdingTaxBrackets.Update;

using FluentValidation;

public sealed class UpdateWithholdingTaxBracketValidator : AbstractValidator<UpdateWithholdingTaxBracketCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public UpdateWithholdingTaxBracketValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
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
