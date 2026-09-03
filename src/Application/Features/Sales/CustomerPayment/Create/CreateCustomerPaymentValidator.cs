namespace ZARI.Application.Features.Sales.CustomerPayments.Create;

using FluentValidation;

public sealed class CreateCustomerPaymentValidator : AbstractValidator<CreateCustomerPaymentCommand>
{
    private static readonly string[] ValidPaymentMethods = ["CASH", "CHECK", "BANK_TRANSFER", "MIXED"];

    public CreateCustomerPaymentValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.PaymentDate).NotEmpty();
        RuleFor(x => x.ReferenceNo).MaximumLength(150);
        RuleFor(x => x.Remarks).MaximumLength(300);

        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one invoice must be selected for payment.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.SalesInvoiceId).NotEmpty();
            line.RuleFor(l => l.AmountApplied).GreaterThan(0);
        });

        // Either the original single-method shape (PaymentMethod + CashAccountId) or a non-empty
        // split-tender list — never neither. A caller supplying both is allowed (Tenders wins server-
        // side; see the handler), so this only rejects the "gave us nothing to fund the payment
        // with" case.
        RuleFor(x => x)
            .Must(x => (!string.IsNullOrWhiteSpace(x.PaymentMethod) && x.CashAccountId.HasValue) || (x.Tenders is { Count: > 0 }))
            .WithMessage("Either PaymentMethod and CashAccountId, or a non-empty Tenders list, must be provided.");

        When(x => !string.IsNullOrWhiteSpace(x.PaymentMethod), () =>
        {
            RuleFor(x => x.PaymentMethod!)
                .Must(m => ValidPaymentMethods.Contains(m))
                .WithMessage($"Payment method must be one of: {string.Join(", ", ValidPaymentMethods)}.");
        });

        RuleForEach(x => x.Tenders).ChildRules(tender =>
        {
            tender.RuleFor(t => t.PaymentMethodId).NotEmpty();
            tender.RuleFor(t => t.Amount).GreaterThan(0);
            tender.RuleFor(t => t.ReferenceNo).MaximumLength(150);
            tender.RuleFor(t => t.BankOrPartnerName).MaximumLength(150);
        });
    }
}
