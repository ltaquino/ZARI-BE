namespace ZARI.Application.Features.Purchasing.ApInvoices.Create;

using FluentValidation;

public sealed class CreateApInvoiceValidator : AbstractValidator<CreateApInvoiceCommand>
{
    private static readonly string[] ValidInvoiceTypes = ["ITEM", "EXPENSE"];

    public CreateApInvoiceValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.InvoiceType)
            .NotEmpty()
            .Must(t => ValidInvoiceTypes.Contains(t))
            .WithMessage($"Invoice type must be one of: {string.Join(", ", ValidInvoiceTypes)}.");
        RuleFor(x => x.SupplierInvoiceNo).NotEmpty().MaximumLength(150);
        RuleFor(x => x.InvoiceDate).NotEmpty();
        RuleFor(x => x.Remarks).MaximumLength(300);

        RuleFor(x => x.GoodsReceiptPoId).NotEmpty().When(x => x.InvoiceType == "ITEM")
            .WithMessage("A goods receipt (PO) is required for an item invoice.");
        RuleFor(x => x.GoodsReceiptPoId).Empty().When(x => x.InvoiceType == "EXPENSE")
            .WithMessage("An expense invoice cannot reference a goods receipt (PO).");

        RuleFor(x => x.Lines).NotEmpty().When(x => x.InvoiceType == "ITEM")
            .WithMessage("At least one item line is required.");
        RuleFor(x => x.Lines).Empty().When(x => x.InvoiceType == "EXPENSE")
            .WithMessage("An expense invoice cannot have item lines.");

        RuleFor(x => x.ExpenseLines).NotEmpty().When(x => x.InvoiceType == "EXPENSE")
            .WithMessage("At least one expense line is required.");
        RuleFor(x => x.ExpenseLines).Empty().When(x => x.InvoiceType == "ITEM")
            .WithMessage("An item invoice cannot have expense lines.");

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
