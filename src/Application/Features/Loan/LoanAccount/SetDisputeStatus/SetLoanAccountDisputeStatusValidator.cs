namespace ZARI.Application.Features.Loan.LoanAccounts.SetDisputeStatus;

using FluentValidation;

public sealed class SetLoanAccountDisputeStatusValidator : AbstractValidator<SetLoanAccountDisputeStatusCommand>
{
    public SetLoanAccountDisputeStatusValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.DisputeNotes).MaximumLength(300);
        RuleFor(x => x.DisputeNotes).NotEmpty().When(x => x.IsDisputed).WithMessage("A note is required when flagging a loan account as disputed.");
    }
}
