namespace ZARI.Application.Features.Loan.LoanPayments.RequestCancellation;

using FluentValidation;

public sealed class RequestLoanPaymentCancellationValidator : AbstractValidator<RequestLoanPaymentCancellationCommand>
{
    public RequestLoanPaymentCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
