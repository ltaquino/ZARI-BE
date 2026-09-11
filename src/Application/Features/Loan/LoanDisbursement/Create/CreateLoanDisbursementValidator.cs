namespace ZARI.Application.Features.Loan.LoanDisbursements.Create;

using FluentValidation;

public sealed class CreateLoanDisbursementValidator : AbstractValidator<CreateLoanDisbursementCommand>
{
    public CreateLoanDisbursementValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.LoanAccountId).NotEmpty();
        RuleFor(x => x.DisbursementDate).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PaymentMethodId).NotEmpty();
        RuleFor(x => x.ReferenceNo).MaximumLength(150);
        RuleFor(x => x.Remarks).MaximumLength(300);
    }
}
