namespace ZARI.Application.Features.Purchasing.ApInvoices.Create;

using FluentValidation;

public sealed class CreateApInvoiceValidator : AbstractValidator<CreateApInvoiceCommand>
{
    public CreateApInvoiceValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.GoodsReceiptPoId).NotEmpty();
        RuleFor(x => x.SupplierInvoiceNo).NotEmpty().MaximumLength(150);
        RuleFor(x => x.InvoiceDate).NotEmpty();
        RuleFor(x => x.Remarks).MaximumLength(300);

        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one item line is required.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId).NotEmpty();
            line.RuleFor(l => l.UomId).NotEmpty();
            line.RuleFor(l => l.Qty).GreaterThan(0);
            line.RuleFor(l => l.UnitCost).GreaterThanOrEqualTo(0);
        });
    }
}
