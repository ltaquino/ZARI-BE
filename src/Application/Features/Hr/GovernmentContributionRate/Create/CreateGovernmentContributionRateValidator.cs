namespace ZARI.Application.Features.Hr.GovernmentContributionRates.Create;

using FluentValidation;

public sealed class CreateGovernmentContributionRateValidator : AbstractValidator<CreateGovernmentContributionRateCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];
    private static readonly string[] ValidContributionTypes = ["PHILHEALTH"];

    public CreateGovernmentContributionRateValidator()
    {
        RuleFor(x => x.ContributionType)
            .NotEmpty()
            .Must(t => ValidContributionTypes.Contains(t))
            .WithMessage($"Contribution type must be one of: {string.Join(", ", ValidContributionTypes)}.");
        RuleFor(x => x.SalaryFrom).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SalaryTo).GreaterThan(x => x.SalaryFrom);
        RuleFor(x => x.EmployeeSharePct).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EmployerSharePct).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxContribution).GreaterThanOrEqualTo(0).When(x => x.MaxContribution is not null);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
