namespace ZARI.Application.Features.Hr.PagIbigContributionBrackets.Create;

using FluentValidation;

public sealed class CreatePagIbigContributionBracketValidator : AbstractValidator<CreatePagIbigContributionBracketCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public CreatePagIbigContributionBracketValidator()
    {
        RuleFor(x => x.SalaryFrom).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SalaryTo).GreaterThan(x => x.SalaryFrom);
        RuleFor(x => x.EmployeeShare).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EmployerShare).GreaterThanOrEqualTo(0);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
