namespace ZARI.Application.Features.Purchasing.ApInvoices.Submit;

using FluentValidation;

public sealed class SubmitApInvoiceValidator : AbstractValidator<SubmitApInvoiceCommand>
{
    public SubmitApInvoiceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
    }
}
