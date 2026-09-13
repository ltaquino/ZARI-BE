namespace ZARI.Application.Features.Loan.LoanDisbursements.ApproveCancellation;

using FluentValidation;

public sealed class ApproveLoanDisbursementCancellationValidator : AbstractValidator<ApproveLoanDisbursementCancellationCommand>
{
    public ApproveLoanDisbursementCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty();
    }
}
