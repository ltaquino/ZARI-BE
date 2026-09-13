namespace ZARI.Application.Features.Loan.LoanApplications.Create;

using FluentValidation;

public sealed class CreateLoanApplicationValidator : AbstractValidator<CreateLoanApplicationCommand>
{
    public CreateLoanApplicationValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.LoanProductId).NotEmpty();
        RuleFor(x => x.ApplicationDate).NotEmpty();
        RuleFor(x => x.RequestedPrincipal).GreaterThan(0);
        RuleFor(x => x.RequestedTermMonths).GreaterThan(0);
        RuleFor(x => x.Purpose).MaximumLength(300);
        RuleFor(x => x.Remarks).MaximumLength(300);

        RuleForEach(x => x.Collaterals).ChildRules(c =>
        {
            c.RuleFor(x => x.Description).NotEmpty().MaximumLength(300);
            c.RuleFor(x => x.CollateralType).NotEmpty().MaximumLength(25);
            c.RuleFor(x => x.EstimatedValue).GreaterThan(0);
        });

        RuleForEach(x => x.CoMakers).ChildRules(c =>
        {
            c.RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        });
    }
}
