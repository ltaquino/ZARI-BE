namespace ZARI.Application.Features.Loan.LoanPayments.ApproveCancellation;

using FluentValidation;

public sealed class ApproveLoanPaymentCancellationValidator : AbstractValidator<ApproveLoanPaymentCancellationCommand>
{
    public ApproveLoanPaymentCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty();
    }
}
