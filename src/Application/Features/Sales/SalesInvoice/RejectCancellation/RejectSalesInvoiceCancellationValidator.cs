namespace ZARI.Application.Features.Sales.SalesInvoices.RejectCancellation;

using FluentValidation;

public sealed class RejectSalesInvoiceCancellationValidator : AbstractValidator<RejectSalesInvoiceCancellationCommand>
{
    public RejectSalesInvoiceCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
