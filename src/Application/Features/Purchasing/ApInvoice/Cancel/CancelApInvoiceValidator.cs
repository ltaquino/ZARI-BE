namespace ZARI.Application.Features.Purchasing.ApInvoices.Cancel;

using FluentValidation;

public sealed class CancelApInvoiceValidator : AbstractValidator<CancelApInvoiceCommand>
{
    public CancelApInvoiceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CancelledBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
