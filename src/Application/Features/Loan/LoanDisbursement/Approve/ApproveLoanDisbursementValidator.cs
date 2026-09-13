namespace ZARI.Application.Features.Loan.LoanDisbursements.Approve;

using FluentValidation;

public sealed class ApproveLoanDisbursementValidator : AbstractValidator<ApproveLoanDisbursementCommand>
{
    public ApproveLoanDisbursementValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty();
    }
}
