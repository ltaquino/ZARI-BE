namespace ZARI.Application.Features.Loan.LoanDisbursements.Cancel;

using FluentValidation;

public sealed class CancelLoanDisbursementValidator : AbstractValidator<CancelLoanDisbursementCommand>
{
    public CancelLoanDisbursementValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CancelledBy).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
