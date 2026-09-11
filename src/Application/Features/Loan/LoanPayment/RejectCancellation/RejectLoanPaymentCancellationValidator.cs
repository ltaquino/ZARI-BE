namespace ZARI.Application.Features.Loan.LoanPayments.RejectCancellation;

using FluentValidation;

public sealed class RejectLoanPaymentCancellationValidator : AbstractValidator<RejectLoanPaymentCancellationCommand>
{
    public RejectLoanPaymentCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty();
        RuleFor(x => x.Comments).NotEmpty();
    }
}
