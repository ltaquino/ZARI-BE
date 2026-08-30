namespace ZARI.Application.Features.Sales.SalesInvoices.Reject;

using FluentValidation;

public sealed class RejectSalesInvoiceValidator : AbstractValidator<RejectSalesInvoiceCommand>
{
    public RejectSalesInvoiceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
