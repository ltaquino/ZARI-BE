namespace ZARI.Application.Features.Loan.LoanApplications.Cancel;

using FluentValidation;

public sealed class CancelLoanApplicationValidator : AbstractValidator<CancelLoanApplicationCommand>
{
    public CancelLoanApplicationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CancelledBy).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().WithMessage("A reason is required to cancel a loan application.");
    }
}
