namespace ZARI.Application.Features.Loan.LoanRestructurings.Approve;

using FluentValidation;

public sealed class ApproveLoanRestructuringValidator : AbstractValidator<ApproveLoanRestructuringCommand>
{
    public ApproveLoanRestructuringValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty();
    }
}
