namespace ZARI.Application.Features.Sales.SalesInvoices.Cancel;

using FluentValidation;

public sealed class CancelSalesInvoiceValidator : AbstractValidator<CancelSalesInvoiceCommand>
{
    public CancelSalesInvoiceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CancelledBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
