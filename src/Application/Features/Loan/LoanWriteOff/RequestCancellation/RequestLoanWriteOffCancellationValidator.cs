namespace ZARI.Application.Features.Loan.LoanWriteOffs.RequestCancellation;

using FluentValidation;

public sealed class RequestLoanWriteOffCancellationValidator : AbstractValidator<RequestLoanWriteOffCancellationCommand>
{
    public RequestLoanWriteOffCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
