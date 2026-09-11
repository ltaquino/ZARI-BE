namespace ZARI.Application.Features.Loan.LoanWriteOffs.Cancel;

using FluentValidation;

public sealed class CancelLoanWriteOffValidator : AbstractValidator<CancelLoanWriteOffCommand>
{
    public CancelLoanWriteOffValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CancelledBy).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
