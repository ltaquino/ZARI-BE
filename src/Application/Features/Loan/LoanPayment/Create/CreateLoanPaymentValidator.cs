namespace ZARI.Application.Features.Loan.LoanPayments.Create;

using FluentValidation;

public sealed class CreateLoanPaymentValidator : AbstractValidator<CreateLoanPaymentCommand>
{
    public CreateLoanPaymentValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.LoanAccountId).NotEmpty();
        RuleFor(x => x.PaymentDate).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PaymentMethodId).NotEmpty();
        RuleFor(x => x.ReferenceNo).MaximumLength(150);
        RuleFor(x => x.Remarks).MaximumLength(300);
    }
}
