namespace ZARI.Application.Features.Loan.LoanWriteOffs.Approve;

using FluentValidation;

public sealed class ApproveLoanWriteOffValidator : AbstractValidator<ApproveLoanWriteOffCommand>
{
    public ApproveLoanWriteOffValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty();
    }
}
