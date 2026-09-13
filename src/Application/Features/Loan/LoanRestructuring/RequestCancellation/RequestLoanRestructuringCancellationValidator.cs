namespace ZARI.Application.Features.Loan.LoanRestructurings.RequestCancellation;

using FluentValidation;

public sealed class RequestLoanRestructuringCancellationValidator : AbstractValidator<RequestLoanRestructuringCancellationCommand>
{
    public RequestLoanRestructuringCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
