namespace ZARI.Application.Features.Sales.SalesInvoices.Create;

using FluentValidation;

public sealed class CreateSalesInvoiceValidator : AbstractValidator<CreateSalesInvoiceCommand>
{
    private static readonly string[] ValidVatTypes = ["VATABLE", "VAT_EXEMPT", "ZERO_RATED"];

    public CreateSalesInvoiceValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.InvoiceDate).NotEmpty();
        RuleFor(x => x.Remarks).MaximumLength(300);
        RuleFor(x => x.DiscountPct).InclusiveBetween(0, 100).When(x => x.DiscountPct.HasValue);

        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one item line is required.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId).NotEmpty();
            line.RuleFor(l => l.UomId).NotEmpty();
            line.RuleFor(l => l.Qty).GreaterThan(0);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.DiscountPct).InclusiveBetween(0, 100);
            line.RuleFor(l => l.VatType).Must(v => ValidVatTypes.Contains(v))
                .WithMessage($"VAT type must be one of: {string.Join(", ", ValidVatTypes)}.");

            // A statutory discount is a legal entitlement, never combined with a discretionary
            // discount on the same line — see SalesInvoiceLineCalculator's own doc comment.
            line.RuleFor(l => l.DiscountPct).Equal(0)
                .When(l => l.StatutoryDiscountTypeId.HasValue)
                .WithMessage("A line with a statutory discount cannot also carry a discretionary discount.");
            line.RuleFor(l => l.StatutoryIdNumber).NotEmpty()
                .When(l => l.StatutoryDiscountTypeId.HasValue)
                .WithMessage("The qualifying ID number is required when a statutory discount is selected.");
        });
    }
}
