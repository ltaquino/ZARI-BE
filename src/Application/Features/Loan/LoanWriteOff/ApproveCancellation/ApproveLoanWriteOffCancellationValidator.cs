namespace ZARI.Application.Features.Loan.LoanWriteOffs.ApproveCancellation;

using FluentValidation;

public sealed class ApproveLoanWriteOffCancellationValidator : AbstractValidator<ApproveLoanWriteOffCancellationCommand>
{
    public ApproveLoanWriteOffCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty();
    }
}
