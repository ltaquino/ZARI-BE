namespace ZARI.Application.Features.Loan.LoanRestructurings.Reject;

using FluentValidation;

public sealed class RejectLoanRestructuringValidator : AbstractValidator<RejectLoanRestructuringCommand>
{
    public RejectLoanRestructuringValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty();
        RuleFor(x => x.Comments).NotEmpty();
    }
}
