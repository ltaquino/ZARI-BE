namespace ZARI.Application.Features.Sales.CustomerPayments.Create;

using FluentValidation;

public sealed class CreateCustomerPaymentValidator : AbstractValidator<CreateCustomerPaymentCommand>
{
    private static readonly string[] ValidPaymentMethods = ["CASH", "CHECK", "BANK_TRANSFER"];

    public CreateCustomerPaymentValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.PaymentMethod).NotEmpty()
            .Must(m => ValidPaymentMethods.Contains(m))
            .WithMessage($"Payment method must be one of: {string.Join(", ", ValidPaymentMethods)}.");
        RuleFor(x => x.CashAccountId).NotEmpty();
        RuleFor(x => x.PaymentDate).NotEmpty();
        RuleFor(x => x.ReferenceNo).MaximumLength(150);
        RuleFor(x => x.Remarks).MaximumLength(300);

        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one invoice must be selected for payment.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.SalesInvoiceId).NotEmpty();
            line.RuleFor(l => l.AmountApplied).GreaterThan(0);
        });
    }
}
