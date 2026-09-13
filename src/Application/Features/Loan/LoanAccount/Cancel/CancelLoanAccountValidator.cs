namespace ZARI.Application.Features.Loan.LoanAccounts.Cancel;

using FluentValidation;

public sealed class CancelLoanAccountValidator : AbstractValidator<CancelLoanAccountCommand>
{
    public CancelLoanAccountValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CancelledBy).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
