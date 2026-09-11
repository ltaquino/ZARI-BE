namespace ZARI.Application.Features.Loan.LoanApplications.Submit;

using FluentValidation;

public sealed class SubmitLoanApplicationValidator : AbstractValidator<SubmitLoanApplicationCommand>
{
    public SubmitLoanApplicationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty();
    }
}
