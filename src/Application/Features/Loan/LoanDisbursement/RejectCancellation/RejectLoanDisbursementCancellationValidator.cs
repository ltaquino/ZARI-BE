namespace ZARI.Application.Features.Loan.LoanDisbursements.RejectCancellation;

using FluentValidation;

public sealed class RejectLoanDisbursementCancellationValidator : AbstractValidator<RejectLoanDisbursementCancellationCommand>
{
    public RejectLoanDisbursementCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty();
        RuleFor(x => x.Comments).NotEmpty();
    }
}
