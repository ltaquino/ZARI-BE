namespace ZARI.Application.Features.Loan.LoanApplications.Approve;

using FluentValidation;

public sealed class ApproveLoanApplicationValidator : AbstractValidator<ApproveLoanApplicationCommand>
{
    public ApproveLoanApplicationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty();
    }
}
