namespace ZARI.Application.Features.Sales.SalesInvoices.ApproveCancellation;

using FluentValidation;

public sealed class ApproveSalesInvoiceCancellationValidator : AbstractValidator<ApproveSalesInvoiceCancellationCommand>
{
    public ApproveSalesInvoiceCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
