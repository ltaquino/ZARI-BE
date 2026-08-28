namespace ZARI.Application.Features.Purchasing.OutgoingPayments.Create;

using FluentValidation;

public sealed class CreateOutgoingPaymentValidator : AbstractValidator<CreateOutgoingPaymentCommand>
{
    public CreateOutgoingPaymentValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.BankAccountId).NotEmpty();
        RuleFor(x => x.PaymentDate).NotEmpty();
        RuleFor(x => x.RefNo).MaximumLength(150);
        RuleFor(x => x.Remarks).MaximumLength(300);

        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one invoice must be selected for payment.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ApInvoiceId).NotEmpty();
            line.RuleFor(l => l.Amount).GreaterThan(0);
        });
    }
}
