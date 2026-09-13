namespace ZARI.Application.Features.Loan.LoanRestructurings.ApproveCancellation;

using FluentValidation;

public sealed class ApproveLoanRestructuringCancellationValidator : AbstractValidator<ApproveLoanRestructuringCancellationCommand>
{
    public ApproveLoanRestructuringCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty();
    }
}
