namespace ZARI.Application.Features.Loan.LoanRestructurings.Submit;

using FluentValidation;

public sealed class SubmitLoanRestructuringValidator : AbstractValidator<SubmitLoanRestructuringCommand>
{
    public SubmitLoanRestructuringValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty();
    }
}
