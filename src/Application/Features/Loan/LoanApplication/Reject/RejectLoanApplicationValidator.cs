namespace ZARI.Application.Features.Loan.LoanApplications.Reject;

using FluentValidation;

public sealed class RejectLoanApplicationValidator : AbstractValidator<RejectLoanApplicationCommand>
{
    public RejectLoanApplicationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty();
        RuleFor(x => x.Comments).NotEmpty().WithMessage("A reason is required when rejecting a loan application.");
    }
}
