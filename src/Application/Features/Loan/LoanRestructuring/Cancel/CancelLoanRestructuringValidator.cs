namespace ZARI.Application.Features.Loan.LoanRestructurings.Cancel;

using FluentValidation;

public sealed class CancelLoanRestructuringValidator : AbstractValidator<CancelLoanRestructuringCommand>
{
    public CancelLoanRestructuringValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CancelledBy).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
