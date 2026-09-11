namespace ZARI.Application.Features.Loan.LoanRestructurings.RejectCancellation;

using FluentValidation;

public sealed class RejectLoanRestructuringCancellationValidator : AbstractValidator<RejectLoanRestructuringCancellationCommand>
{
    public RejectLoanRestructuringCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty();
        RuleFor(x => x.Comments).NotEmpty();
    }
}
