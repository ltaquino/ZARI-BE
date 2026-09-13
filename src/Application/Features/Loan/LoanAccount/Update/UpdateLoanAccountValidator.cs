namespace ZARI.Application.Features.Loan.LoanAccounts.Update;

using FluentValidation;

public sealed class UpdateLoanAccountValidator : AbstractValidator<UpdateLoanAccountCommand>
{
    public UpdateLoanAccountValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.LoanProductId).NotEmpty();
        RuleFor(x => x.PrincipalAmount).GreaterThan(0);
        RuleFor(x => x.TermMonths).GreaterThan(0);
        RuleFor(x => x.GrantDate).NotEmpty();
        RuleFor(x => x.FirstDueDate).NotEmpty();
        RuleFor(x => x.Remarks).MaximumLength(300);
    }
}
