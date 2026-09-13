namespace ZARI.Application.Features.Loan.LoanDisbursements.Submit;

using FluentValidation;

public sealed class SubmitLoanDisbursementValidator : AbstractValidator<SubmitLoanDisbursementCommand>
{
    public SubmitLoanDisbursementValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty();
    }
}
