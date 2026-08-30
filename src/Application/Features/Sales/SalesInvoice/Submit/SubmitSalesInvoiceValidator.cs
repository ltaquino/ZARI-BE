namespace ZARI.Application.Features.Sales.SalesInvoices.Submit;

using FluentValidation;

public sealed class SubmitSalesInvoiceValidator : AbstractValidator<SubmitSalesInvoiceCommand>
{
    public SubmitSalesInvoiceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
    }
}
