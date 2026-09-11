namespace ZARI.Application.Features.Loan.LoanWriteOffs.Submit;

using FluentValidation;

public sealed class SubmitLoanWriteOffValidator : AbstractValidator<SubmitLoanWriteOffCommand>
{
    public SubmitLoanWriteOffValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty();
    }
}
