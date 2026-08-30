namespace ZARI.Application.Features.Sales.SalesInvoices.RequestCancellation;

using FluentValidation;

public sealed class RequestSalesInvoiceCancellationValidator : AbstractValidator<RequestSalesInvoiceCancellationCommand>
{
    public RequestSalesInvoiceCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
