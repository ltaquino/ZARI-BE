namespace ZARI.Application.Features.Purchasing.ApInvoices.Update;

using FluentValidation;

/// <summary>
/// InvoiceType isn't part of this command (it's immutable post-creation), so this validator can't
/// branch on it the way Create's does — it just requires at least one of Lines/ExpenseLines to be
/// non-empty. The handler enforces that the populated list actually matches the invoice's own
/// stored InvoiceType.
/// </summary>
public sealed class UpdateApInvoiceValidator : AbstractValidator<UpdateApInvoiceCommand>
{
    public UpdateApInvoiceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.SupplierInvoiceNo).NotEmpty().MaximumLength(150);
        RuleFor(x => x.InvoiceDate).NotEmpty();
        RuleFor(x => x.Remarks).MaximumLength(300);

        RuleFor(x => x)
            .Must(x => x.Lines.Count > 0 || x.ExpenseLines.Count > 0)
            .WithMessage("At least one line is required.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId).NotEmpty();
            line.RuleFor(l => l.UomId).NotEmpty();
            line.RuleFor(l => l.Qty).GreaterThan(0);
            line.RuleFor(l => l.UnitCost).GreaterThanOrEqualTo(0);
        });

        RuleForEach(x => x.ExpenseLines).ChildRules(line =>
        {
            line.RuleFor(l => l.GlAccountId).NotEmpty();
            line.RuleFor(l => l.Description).NotEmpty().MaximumLength(300);
            line.RuleFor(l => l.Amount).GreaterThan(0);
        });
    }
}
