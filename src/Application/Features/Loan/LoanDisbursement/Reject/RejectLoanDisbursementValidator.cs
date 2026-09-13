namespace ZARI.Application.Features.Loan.LoanDisbursements.Reject;

using FluentValidation;

public sealed class RejectLoanDisbursementValidator : AbstractValidator<RejectLoanDisbursementCommand>
{
    public RejectLoanDisbursementValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty();
        RuleFor(x => x.Comments).NotEmpty();
    }
}
