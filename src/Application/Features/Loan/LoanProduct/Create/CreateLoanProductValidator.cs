namespace ZARI.Application.Features.Loan.LoanProducts.Create;

using FluentValidation;

public sealed class CreateLoanProductValidator : AbstractValidator<CreateLoanProductCommand>
{
    private static readonly string[] ValidInterestMethods = ["DIMINISHING", "FLAT_ADD_ON"];
    private static readonly string[] ValidFrequencies = ["MONTHLY", "SEMI_MONTHLY", "WEEKLY"];
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public CreateLoanProductValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);

        RuleFor(x => x.InterestMethod).NotEmpty().Must(m => ValidInterestMethods.Contains(m))
            .WithMessage($"Interest method must be one of: {string.Join(", ", ValidInterestMethods)}.");
        RuleFor(x => x.AnnualInterestRatePct).InclusiveBetween(0, 100);

        RuleFor(x => x.MinPrincipal).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxPrincipal).GreaterThan(0);
        RuleFor(x => x).Must(x => x.MaxPrincipal >= x.MinPrincipal).WithMessage("Maximum principal must be at least the minimum principal.");

        RuleFor(x => x.MinTermMonths).GreaterThan(0);
        RuleFor(x => x.MaxTermMonths).GreaterThan(0);
        RuleFor(x => x).Must(x => x.MaxTermMonths >= x.MinTermMonths).WithMessage("Maximum term must be at least the minimum term.");

        RuleFor(x => x.RepaymentFrequency).NotEmpty().Must(f => ValidFrequencies.Contains(f))
            .WithMessage($"Repayment frequency must be one of: {string.Join(", ", ValidFrequencies)}.");

        RuleFor(x => x.GracePeriodDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PenaltyRatePct).GreaterThanOrEqualTo(0);

        RuleFor(x => x.Status).NotEmpty().Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
