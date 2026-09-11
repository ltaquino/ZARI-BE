namespace ZARI.Application.Features.Loan.LoanDisbursements.RequestCancellation;

using FluentValidation;

public sealed class RequestLoanDisbursementCancellationValidator : AbstractValidator<RequestLoanDisbursementCancellationCommand>
{
    public RequestLoanDisbursementCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
