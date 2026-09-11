namespace ZARI.Application.Features.Loan.LoanWriteOffs.Reject;

using FluentValidation;

public sealed class RejectLoanWriteOffValidator : AbstractValidator<RejectLoanWriteOffCommand>
{
    public RejectLoanWriteOffValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty();
        RuleFor(x => x.Comments).NotEmpty();
    }
}
