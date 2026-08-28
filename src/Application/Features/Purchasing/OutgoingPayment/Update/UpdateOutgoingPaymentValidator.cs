namespace ZARI.Application.Features.Purchasing.OutgoingPayments.Update;

using FluentValidation;

public sealed class UpdateOutgoingPaymentValidator : AbstractValidator<UpdateOutgoingPaymentCommand>
{
    public UpdateOutgoingPaymentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
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
