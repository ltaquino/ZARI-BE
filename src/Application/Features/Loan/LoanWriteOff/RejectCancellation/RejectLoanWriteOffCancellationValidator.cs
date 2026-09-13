namespace ZARI.Application.Features.Loan.LoanWriteOffs.RejectCancellation;

using FluentValidation;

public sealed class RejectLoanWriteOffCancellationValidator : AbstractValidator<RejectLoanWriteOffCancellationCommand>
{
    public RejectLoanWriteOffCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty();
        RuleFor(x => x.Comments).NotEmpty();
    }
}
