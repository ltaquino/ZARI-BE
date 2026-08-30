namespace ZARI.Application.Features.Sales.SalesInvoices.Approve;

using FluentValidation;

public sealed class ApproveSalesInvoiceValidator : AbstractValidator<ApproveSalesInvoiceCommand>
{
    public ApproveSalesInvoiceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
