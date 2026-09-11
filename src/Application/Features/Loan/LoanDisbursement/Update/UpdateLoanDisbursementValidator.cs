namespace ZARI.Application.Features.Loan.LoanDisbursements.Update;

using FluentValidation;

public sealed class UpdateLoanDisbursementValidator : AbstractValidator<UpdateLoanDisbursementCommand>
{
    public UpdateLoanDisbursementValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.DisbursementDate).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PaymentMethodId).NotEmpty();
        RuleFor(x => x.ReferenceNo).MaximumLength(150);
        RuleFor(x => x.Remarks).MaximumLength(300);
    }
}
