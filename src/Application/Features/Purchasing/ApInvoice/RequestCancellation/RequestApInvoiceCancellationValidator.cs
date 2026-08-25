namespace ZARI.Application.Features.Purchasing.ApInvoices.RequestCancellation;

using FluentValidation;

public sealed class RequestApInvoiceCancellationValidator : AbstractValidator<RequestApInvoiceCancellationCommand>
{
    public RequestApInvoiceCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
