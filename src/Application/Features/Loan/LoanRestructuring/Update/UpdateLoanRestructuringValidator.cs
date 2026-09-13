namespace ZARI.Application.Features.Loan.LoanRestructurings.Update;

using FluentValidation;

public sealed class UpdateLoanRestructuringValidator : AbstractValidator<UpdateLoanRestructuringCommand>
{
    private static readonly string[] ValidFrequencies = ["MONTHLY", "SEMI_MONTHLY", "WEEKLY"];

    public UpdateLoanRestructuringValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.RestructureDate).NotEmpty();
        RuleFor(x => x.NewAnnualInterestRatePct).GreaterThanOrEqualTo(0);
        RuleFor(x => x.NewTermMonths).GreaterThan(0);
        RuleFor(x => x.NewRepaymentFrequency).Must(f => ValidFrequencies.Contains(f)).WithMessage($"Repayment frequency must be one of: {string.Join(", ", ValidFrequencies)}.");
        RuleFor(x => x.NewGracePeriodDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.NewPenaltyRatePct).GreaterThanOrEqualTo(0);
        RuleFor(x => x.NewFirstDueDate).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Remarks).MaximumLength(300);
    }
}
