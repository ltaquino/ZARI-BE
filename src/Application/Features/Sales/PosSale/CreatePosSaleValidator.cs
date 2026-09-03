namespace ZARI.Application.Features.Sales.PosSale;

using FluentValidation;

public sealed class CreatePosSaleValidator : AbstractValidator<CreatePosSaleCommand>
{
    public CreatePosSaleValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.PosTerminalId).NotEmpty();
        RuleFor(x => x.InvoiceDate).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one item must be scanned before checkout.");
        RuleFor(x => x.Tenders).NotEmpty().WithMessage("At least one payment method must be applied before checkout.");

        RuleForEach(x => x.Tenders).ChildRules(tender =>
        {
            tender.RuleFor(t => t.PaymentMethodId).NotEmpty();
            tender.RuleFor(t => t.Amount).GreaterThan(0);
        });
    }
}
