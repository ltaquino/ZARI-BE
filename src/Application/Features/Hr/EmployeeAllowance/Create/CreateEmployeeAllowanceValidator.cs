namespace ZARI.Application.Features.Hr.EmployeeAllowances.Create;

using FluentValidation;

public sealed class CreateEmployeeAllowanceValidator : AbstractValidator<CreateEmployeeAllowanceCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public CreateEmployeeAllowanceValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.EffectiveDate).When(x => x.EndDate is not null);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
